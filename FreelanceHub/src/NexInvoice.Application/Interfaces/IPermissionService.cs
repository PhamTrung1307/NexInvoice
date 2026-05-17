namespace NexInvoice.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default);
}
