using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class Client : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public Guid? OwnerId { get; set; }

    public AppUser? Owner { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
