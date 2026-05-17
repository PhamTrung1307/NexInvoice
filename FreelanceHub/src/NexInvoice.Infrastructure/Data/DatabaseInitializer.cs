using Bogus;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data.FakeData;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Data;

internal sealed class DatabaseInitializer
{
    private const int TargetUserCount = 30;
    private const int TargetClientCount = 50;
    private const int TargetProjectCount = 120;
    private const int TargetTaskCount = 1_000;
    private const int TargetInvoiceCount = 300;
    private const int TargetPaymentCount = 500;
    private const int TargetNotificationCount = 1_000;

    private readonly AppDbContext _dbContext;
    private readonly AppDbContextSeeder _systemSeeder;

    public DatabaseInitializer(AppDbContext dbContext, AppDbContextSeeder systemSeeder)
    {
        _dbContext = dbContext;
        _systemSeeder = systemSeeder;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
        await _systemSeeder.SeedAsync(cancellationToken);

        if (await HasBusinessDataAsync(cancellationToken))
        {
            return;
        }

        await SeedFakeBusinessDataAsync(cancellationToken);
    }

    private async Task<bool> HasBusinessDataAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Clients.AnyAsync(cancellationToken)
            || await _dbContext.Projects.AnyAsync(cancellationToken)
            || await _dbContext.TaskItems.AnyAsync(cancellationToken)
            || await _dbContext.Invoices.AnyAsync(cancellationToken)
            || await _dbContext.Payments.AnyAsync(cancellationToken)
            || await _dbContext.Notifications.AnyAsync(cancellationToken);
    }

    private async Task SeedFakeBusinessDataAsync(CancellationToken cancellationToken)
    {
        var users = await EnsureFakeUsersAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();

        var clients = new ClientFakeDataGenerator()
            .Generate(TargetClientCount, userIds)
            .ToArray();
        _dbContext.Clients.AddRange(clients);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var activeClients = clients
            .Where(client => client.Status == ClientStatus.Active)
            .ToArray();

        var projects = new ProjectFakeDataGenerator()
            .Generate(TargetProjectCount, activeClients, userIds)
            .ToArray();
        _dbContext.Projects.AddRange(projects);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tasks = new TaskFakeDataGenerator()
            .Generate(TargetTaskCount, projects, userIds)
            .ToArray();
        _dbContext.TaskItems.AddRange(tasks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AlignCompletedProjectStatuses(projects, tasks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var invoices = new InvoiceFakeDataGenerator()
            .Generate(TargetInvoiceCount, projects)
            .ToArray();
        _dbContext.Invoices.AddRange(invoices);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var payments = new PaymentFakeDataGenerator()
            .Generate(TargetPaymentCount, invoices)
            .ToArray();
        _dbContext.Payments.AddRange(payments);
        ApplyInvoicePaymentStatuses(invoices, payments);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var notifications = new NotificationFakeDataGenerator()
            .Generate(TargetNotificationCount, users)
            .ToArray();
        _dbContext.Notifications.AddRange(notifications);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AppUser>> EnsureFakeUsersAsync(CancellationToken cancellationToken)
    {
        var existingUsers = await _dbContext.AppUsers
            .ToArrayAsync(cancellationToken);

        var missingCount = Math.Max(0, TargetUserCount - existingUsers.Length);

        if (missingCount == 0)
        {
            return existingUsers;
        }

        var faker = new Faker("vi");
        var users = Enumerable.Range(1, missingCount)
            .Select(index => new AppUser
            {
                FullName = VietnameseFakeData.FullName(faker),
                Email = $"user{existingUsers.Length + index:000}@nexinvoice.test",
                PhoneNumber = VietnameseFakeData.PhoneNumber(faker),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                IsActive = faker.Random.Bool(0.92f),
                LastLoginAt = DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 180))
            })
            .ToArray();

        _dbContext.AppUsers.AddRange(users);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await AssignFakeUserRolesAsync(users, cancellationToken);

        return existingUsers.Concat(users).ToArray();
    }

    private async Task AssignFakeUserRolesAsync(
        IReadOnlyCollection<AppUser> users,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .Where(role => role.Name == AppRoles.Freelancer || role.Name == AppRoles.Client)
            .ToDictionaryAsync(role => role.Name, cancellationToken);
        var faker = new Faker();

        foreach (var user in users)
        {
            var roleName = faker.Random.Bool(0.72f) ? AppRoles.Freelancer : AppRoles.Client;

            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roles[roleName].Id
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AlignCompletedProjectStatuses(
        IReadOnlyCollection<Project> projects,
        IReadOnlyCollection<TaskItem> tasks)
    {
        var tasksByProject = tasks
            .GroupBy(task => task.ProjectId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var project in projects.Where(project => project.Status == ProjectStatus.Completed))
        {
            if (!tasksByProject.TryGetValue(project.Id, out var projectTasks))
            {
                continue;
            }

            foreach (var task in projectTasks)
            {
                task.Status = NexInvoice.Domain.Enums.TaskStatus.Done;
            }
        }
    }

    private static void ApplyInvoicePaymentStatuses(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var confirmedPaymentsByInvoice = payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .GroupBy(payment => payment.InvoiceId)
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

        foreach (var invoice in invoices)
        {
            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                continue;
            }

            var totalConfirmed = confirmedPaymentsByInvoice.GetValueOrDefault(invoice.Id);

            if (totalConfirmed >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (totalConfirmed > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            else if (invoice.DueDate.HasValue && invoice.DueDate.Value < today)
            {
                invoice.Status = InvoiceStatus.Overdue;
            }
            else
            {
                invoice.Status = InvoiceStatus.Sent;
            }
        }
    }
}
