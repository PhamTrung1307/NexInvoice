using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Interfaces;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Identity;

internal sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _dbContext;

    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return await _dbContext.UserRoles
            .AnyAsync(userRole =>
                userRole.UserId == userId
                && userRole.Role != null
                && (
                    userRole.Role.Name == AppRoles.Admin
                    || userRole.Role.RolePermissions.Any(rolePermission =>
                        rolePermission.Permission != null
                        && rolePermission.Permission.Code == permission)
                ),
                cancellationToken);
    }
}
