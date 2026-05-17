using NexInvoice.Application.Features.Notifications;

namespace NexInvoice.Application.Interfaces;

public interface IRealtimeNotificationService
{
    Task SendToUserAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);
}
