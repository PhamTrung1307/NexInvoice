using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal Budget { get; set; }

    public Guid ClientId { get; set; }

    public Client? Client { get; set; }

    public Guid? OwnerId { get; set; }

    public AppUser? Owner { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
