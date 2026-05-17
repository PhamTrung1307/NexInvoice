using NexInvoice.API.Hubs;
using NexInvoice.Application.Features.Notifications;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace NexInvoice.API.Services;

public sealed class SignalRRealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRRealtimeNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("NotificationReceived", notification, cancellationToken);
    }
}
