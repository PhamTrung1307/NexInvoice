using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsRevoked { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }
}
