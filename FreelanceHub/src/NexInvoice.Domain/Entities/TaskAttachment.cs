using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class TaskAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long SizeInBytes { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    public Guid UploadedById { get; set; }

    public AppUser? UploadedBy { get; set; }
}
