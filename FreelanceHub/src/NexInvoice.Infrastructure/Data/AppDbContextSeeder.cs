using NexInvoice.Application.Common.Authorization;
using NexInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Data;

internal sealed class AppDbContextSeeder
{
    private const string AdminEmail = "admin@nexinvoice.com";
    private const string AdminPassword = "Admin@123";
    private const string StaffEmail = "staff@nexinvoice.com";
    private const string StaffPassword = "Staff@123";
    private const string CustomerEmail = "customer@nexinvoice.com";
    private const string CustomerPassword = "Customer@123";

    private readonly AppDbContext _dbContext;

    public AppDbContextSeeder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedDemoUsersAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            AppRoles.Admin,
            AppRoles.Freelancer,
            AppRoles.Client
        };

        foreach (var roleName in roles)
        {
            var exists = await _dbContext.Roles
                .AnyAsync(role => role.Name == roleName, cancellationToken);

            if (!exists)
            {
                _dbContext.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = roleName
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (var permissionCode in AppPermissions.All)
        {
            var exists = await _dbContext.Permissions
                .AnyAsync(permission => permission.Code == permissionCode, cancellationToken);

            if (!exists)
            {
                _dbContext.Permissions.Add(new Permission
                {
                    Name = permissionCode,
                    Code = permissionCode,
                    Description = permissionCode
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        await AssignPermissionsAsync(AppRoles.Admin, AppPermissions.All, cancellationToken);
        await AssignPermissionsAsync(
            AppRoles.Freelancer,
            new[]
            {
                AppPermissions.ClientView,
                AppPermissions.ClientCreate,
                AppPermissions.ClientUpdate,
                AppPermissions.ProjectView,
                AppPermissions.ProjectCreate,
                AppPermissions.ProjectUpdate,
                AppPermissions.TaskView,
                AppPermissions.TaskCreate,
                AppPermissions.TaskUpdate,
                AppPermissions.InvoiceView,
                AppPermissions.InvoiceCreate,
                AppPermissions.InvoiceUpdate,
                AppPermissions.PaymentView,
                AppPermissions.PaymentCreate,
                AppPermissions.PaymentUpdate,
                AppPermissions.DashboardView
            },
            cancellationToken);
        await AssignPermissionsAsync(
            AppRoles.Client,
            new[]
            {
                AppPermissions.ProjectView,
                AppPermissions.TaskView,
                AppPermissions.InvoiceView,
                AppPermissions.PaymentView,
                AppPermissions.DashboardView
            },
            cancellationToken);
    }

    private async Task AssignPermissionsAsync(
        string roleName,
        IEnumerable<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstAsync(currentRole => currentRole.Name == roleName, cancellationToken);
        var permissions = await _dbContext.Permissions
            .Where(permission => permissionCodes.Contains(permission.Code))
            .ToArrayAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            var exists = await _dbContext.RolePermissions
                .AnyAsync(rolePermission =>
                    rolePermission.RoleId == role.Id
                    && rolePermission.PermissionId == permission.Id,
                    cancellationToken);

            if (!exists)
            {
                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoUsersAsync(CancellationToken cancellationToken)
    {
        await SeedUserAsync(
            "System Administrator",
            AdminEmail,
            AdminPassword,
            AppRoles.Admin,
            cancellationToken);

        await SeedUserAsync(
            "NexInvoice Staff",
            StaffEmail,
            StaffPassword,
            AppRoles.Freelancer,
            cancellationToken);

        await SeedUserAsync(
            "NexInvoice Customer",
            CustomerEmail,
            CustomerPassword,
            AppRoles.Client,
            cancellationToken);
    }

    private async Task SeedUserAsync(
        string fullName,
        string email,
        string password,
        string roleName,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstAsync(currentRole => currentRole.Name == roleName, cancellationToken);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(currentUser => currentUser.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                FullName = fullName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true
            };

            _dbContext.AppUsers.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await _dbContext.UserRoles
            .AnyAsync(userRole =>
                userRole.UserId == user.Id
                && userRole.RoleId == role.Id,
                cancellationToken);

        if (!hasRole)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
