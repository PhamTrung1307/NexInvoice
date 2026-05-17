using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class AppUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<Client> Clients { get; set; } = new List<Client>();

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
