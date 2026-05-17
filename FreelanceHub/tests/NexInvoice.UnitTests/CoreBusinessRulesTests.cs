using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Features.Invoices;
using NexInvoice.Application.Features.Notifications;
using NexInvoice.Application.Features.Payments;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using NexInvoice.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;

namespace NexInvoice.UnitTests;

public sealed class CoreBusinessRulesTests
{
    [Fact]
    public async Task CreateInvoiceAsync_CalculatesTotalCorrectly()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var invoiceService = CreateInvoiceService(dbContext);
        var project = await SeedProjectAsync(dbContext);

        var request = new CreateInvoiceRequest(
            "INV-001",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 15),
            project.Id,
            TaxAmount: 20m,
            DiscountAmount: 5m,
            new[]
            {
                new InvoiceItemRequest("Thiết kế", 2m, 100m),
                new InvoiceItemRequest("Triển khai", 1m, 50m)
            });

        // Act
        var result = await invoiceService.CreateAsync(request);

        // Assert
        Assert.Equal(250m, result.Subtotal);
        Assert.Equal(20m, result.TaxAmount);
        Assert.Equal(5m, result.DiscountAmount);
        Assert.Equal(265m, result.TotalAmount);
    }

    [Fact]
    public async Task MarkPaidAsync_WhenInvoiceIsCancelled_ThrowsBadRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var invoiceService = CreateInvoiceService(dbContext);
        var invoice = await SeedInvoiceAsync(dbContext, status: InvoiceStatus.Cancelled);

        // Act
        var act = () => invoiceService.MarkPaidAsync(invoice.Id);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(act);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenDueDateIsBeforeIssueDate_ThrowsBadRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var invoiceService = CreateInvoiceService(dbContext);
        var project = await SeedProjectAsync(dbContext);

        var request = new CreateInvoiceRequest(
            "INV-002",
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 5, 9),
            project.Id,
            TaxAmount: 0m,
            DiscountAmount: 0m,
            new[] { new InvoiceItemRequest("Tư vấn", 1m, 100m) });

        // Act
        var act = () => invoiceService.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(act);
    }

    [Fact]
    public async Task UpdateProjectStatusAsync_WhenProjectHasUnfinishedTasks_ThrowsBadRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var projectService = new ProjectService(dbContext);
        var project = await SeedProjectAsync(dbContext);

        dbContext.TaskItems.Add(new TaskItem
        {
            Title = "Chưa hoàn tất",
            Status = DomainTaskStatus.InProgress,
            ProjectId = project.Id
        });
        await dbContext.SaveChangesAsync();

        // Act
        var act = () => projectService.UpdateStatusAsync(
            project.Id,
            new Application.Features.Projects.UpdateProjectStatusRequest(ProjectStatus.Completed));

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(act);
    }

    [Fact]
    public async Task ConfirmAsync_WhenConfirmedPaymentsEqualInvoiceTotal_MarksInvoiceAsPaid()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var paymentService = CreatePaymentService(dbContext);
        var invoice = await SeedInvoiceAsync(dbContext, totalAmount: 100m, status: InvoiceStatus.Sent);

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 100m,
            Method = PaymentMethod.BankTransfer,
            Status = PaymentStatus.Pending,
            PaymentDate = new DateOnly(2026, 5, 16)
        };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        // Act
        await paymentService.ConfirmAsync(payment.Id, Guid.NewGuid());

        // Assert
        var updatedInvoice = await dbContext.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatus.Paid, updatedInvoice.Status);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenAmountIsNotPositive_ThrowsBadRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var paymentService = CreatePaymentService(dbContext);
        var invoice = await SeedInvoiceAsync(dbContext);

        var request = new CreatePaymentRequest(
            invoice.Id,
            Amount: 0m,
            PaymentMethod.BankTransfer,
            new DateOnly(2026, 5, 16),
            TransactionReference: null);

        // Act
        var act = () => paymentService.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(act);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static InvoiceService CreateInvoiceService(AppDbContext dbContext)
    {
        return new InvoiceService(dbContext, CreateCache());
    }

    private static PaymentService CreatePaymentService(AppDbContext dbContext)
    {
        return new PaymentService(
            dbContext,
            new FakeWebHostEnvironment(),
            new FakeRealtimeNotificationService(),
            CreateCache());
    }

    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
    }

    private static async Task<Project> SeedProjectAsync(AppDbContext dbContext)
    {
        var client = new Client
        {
            Name = "Nguyễn Văn A",
            Email = $"client-{Guid.NewGuid():N}@example.com",
            Status = ClientStatus.Active
        };

        var project = new Project
        {
            Name = "Website Freelance",
            Client = client,
            ClientId = client.Id,
            Status = ProjectStatus.Active,
            Budget = 1_000m
        };

        dbContext.Clients.Add(client);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        return project;
    }

    private static async Task<Invoice> SeedInvoiceAsync(
        AppDbContext dbContext,
        decimal totalAmount = 100m,
        InvoiceStatus status = InvoiceStatus.Draft)
    {
        var project = await SeedProjectAsync(dbContext);
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            IssueDate = new DateOnly(2026, 5, 1),
            DueDate = new DateOnly(2026, 5, 15),
            ClientId = project.ClientId,
            ProjectId = project.Id,
            Subtotal = totalAmount,
            TaxAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = totalAmount,
            Status = status
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        return invoice;
    }

    private sealed class FakeRealtimeNotificationService : IRealtimeNotificationService
    {
        public Task SendToUserAsync(
            Guid userId,
            NotificationResponse notification,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "NexInvoice.UnitTests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = Path.GetTempPath();

        public string EnvironmentName { get; set; } = "UnitTests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
