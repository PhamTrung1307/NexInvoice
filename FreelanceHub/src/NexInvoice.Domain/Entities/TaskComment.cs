using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class TaskComment : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public Guid TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    public Guid AuthorId { get; set; }

    public AppUser? Author { get; set; }
}
