using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }
}
