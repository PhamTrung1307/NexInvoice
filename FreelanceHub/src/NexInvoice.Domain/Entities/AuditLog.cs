using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public Guid? UserId { get; set; }

    public AppUser? User { get; set; }
}
