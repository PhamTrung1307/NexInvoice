using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);
