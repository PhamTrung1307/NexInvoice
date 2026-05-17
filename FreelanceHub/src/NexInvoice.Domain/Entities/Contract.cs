using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class Contract : BaseEntity
{
    public string ContractNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal Amount { get; set; }

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public string? FileContentType { get; set; }

    public long? FileSizeInBytes { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public string? RejectReason { get; set; }

    public Guid ClientId { get; set; }

    public Client? Client { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }
}
