using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }
}
