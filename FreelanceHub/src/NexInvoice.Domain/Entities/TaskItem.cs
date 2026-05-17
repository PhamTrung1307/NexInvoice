using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DomainTaskStatus Status { get; set; } = DomainTaskStatus.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateOnly? DueDate { get; set; }

    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? AssignedToId { get; set; }

    public AppUser? AssignedTo { get; set; }

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
