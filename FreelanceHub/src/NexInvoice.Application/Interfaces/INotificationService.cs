using NexInvoice.Application.Features.Notifications;

namespace NexInvoice.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationResponse>> GetCurrentUserNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationResponse> MarkAsReadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
