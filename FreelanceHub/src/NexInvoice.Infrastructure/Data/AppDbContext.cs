using System.Linq.Expressions;
using NexInvoice.Domain.Common;
using NexInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();

    public DbSet<SystemPreference> SystemPreferences => Set<SystemPreference>();

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureBaseEntities(modelBuilder);
        ConfigureIdentity(modelBuilder);
        ConfigureClients(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureTasks(modelBuilder);
        ConfigureContracts(modelBuilder);
        ConfigureInvoices(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureSettings(modelBuilder);
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            builder.HasKey(nameof(BaseEntity.Id));
            builder.Property(nameof(BaseEntity.CreatedAt))
                .HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(nameof(BaseEntity.IsDeleted))
                .HasDefaultValue(false);

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Equal(isDeleted, Expression.Constant(false)), parameter);

            builder.HasQueryFilter(filter);
        }
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");

            entity.Property(user => user.FullName).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(50);

            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.Property(role => role.Name).HasMaxLength(100).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(500);

            entity.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.Property(permission => permission.Name).HasMaxLength(150).IsRequired();
            entity.Property(permission => permission.Code).HasMaxLength(150).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(500);

            entity.HasIndex(permission => permission.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();

            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasIndex(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId }).IsUnique();

            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureClients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");

            entity.Property(client => client.Name).HasMaxLength(200).IsRequired();
            entity.Property(client => client.CompanyName).HasMaxLength(200);
            entity.Property(client => client.Email).HasMaxLength(256).IsRequired();
            entity.Property(client => client.PhoneNumber).HasMaxLength(50);
            entity.Property(client => client.Address).HasMaxLength(500);

            entity.HasIndex(client => client.Email);

            entity.HasOne(client => client.Owner)
                .WithMany(user => user.Clients)
                .HasForeignKey(client => client.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");

            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000);
            entity.Property(project => project.Budget).HasPrecision(18, 2);

            entity.HasOne(project => project.Client)
                .WithMany(client => client.Projects)
                .HasForeignKey(project => project.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(project => project.Owner)
                .WithMany(user => user.OwnedProjects)
                .HasForeignKey(project => project.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTasks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("TaskItems");

            entity.Property(task => task.Title).HasMaxLength(250).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(4000);

            entity.HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(task => task.AssignedTo)
                .WithMany(user => user.AssignedTasks)
                .HasForeignKey(task => task.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");

            entity.Property(comment => comment.Content).HasMaxLength(4000).IsRequired();

            entity.HasOne(comment => comment.TaskItem)
                .WithMany(task => task.Comments)
                .HasForeignKey(comment => comment.TaskItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(comment => comment.Author)
                .WithMany(user => user.TaskComments)
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            entity.ToTable("TaskAttachments");

            entity.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.FileUrl).HasMaxLength(1000).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(150);

            entity.HasOne(attachment => attachment.TaskItem)
                .WithMany(task => task.Attachments)
                .HasForeignKey(attachment => attachment.TaskItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(attachment => attachment.UploadedBy)
                .WithMany()
                .HasForeignKey(attachment => attachment.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureContracts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts");

            entity.Property(contract => contract.ContractNumber).HasMaxLength(100).IsRequired();
            entity.Property(contract => contract.Title).HasMaxLength(250).IsRequired();
            entity.Property(contract => contract.Description).HasMaxLength(4000);
            entity.Property(contract => contract.Amount).HasPrecision(18, 2);
            entity.Property(contract => contract.FileName).HasMaxLength(255);
            entity.Property(contract => contract.FileUrl).HasMaxLength(1000);
            entity.Property(contract => contract.FileContentType).HasMaxLength(150);
            entity.Property(contract => contract.RejectReason).HasMaxLength(1000);

            entity.HasIndex(contract => contract.ContractNumber).IsUnique();

            entity.HasOne(contract => contract.Client)
                .WithMany(client => client.Contracts)
                .HasForeignKey(contract => contract.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(contract => contract.Project)
                .WithMany(project => project.Contracts)
                .HasForeignKey(contract => contract.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");

            entity.Property(invoice => invoice.InvoiceNumber).HasMaxLength(100).IsRequired();
            entity.Property(invoice => invoice.Subtotal).HasPrecision(18, 2);
            entity.Property(invoice => invoice.TaxAmount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.DiscountAmount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);

            entity.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();

            entity.HasOne(invoice => invoice.Client)
                .WithMany(client => client.Invoices)
                .HasForeignKey(invoice => invoice.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(invoice => invoice.Project)
                .WithMany(project => project.Invoices)
                .HasForeignKey(invoice => invoice.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems");

            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 2);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.Amount).HasPrecision(18, 2);

            entity.HasOne(item => item.Invoice)
                .WithMany(invoice => invoice.Items)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.TransactionReference).HasMaxLength(150);
            entity.Property(payment => payment.ProofFileName).HasMaxLength(255);
            entity.Property(payment => payment.ProofFileUrl).HasMaxLength(1000);
            entity.Property(payment => payment.ProofContentType).HasMaxLength(150);
            entity.Property(payment => payment.RejectReason).HasMaxLength(1000);

            entity.HasOne(payment => payment.Invoice)
                .WithMany(invoice => invoice.Payments)
                .HasForeignKey(payment => payment.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.Property(notification => notification.Title).HasMaxLength(250).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();

            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.Property(refreshToken => refreshToken.Token).HasMaxLength(500).IsRequired();

            entity.HasIndex(refreshToken => refreshToken.Token).IsUnique();

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.Property(auditLog => auditLog.EntityName).HasMaxLength(200).IsRequired();
            entity.Property(auditLog => auditLog.Action).HasMaxLength(100).IsRequired();
            entity.Property(auditLog => auditLog.OldValues).HasMaxLength(4000);
            entity.Property(auditLog => auditLog.NewValues).HasMaxLength(4000);

            entity.HasOne(auditLog => auditLog.User)
                .WithMany()
                .HasForeignKey(auditLog => auditLog.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyProfile>(entity =>
        {
            entity.ToTable("CompanyProfiles");

            entity.Property(profile => profile.CompanyName).HasMaxLength(250).IsRequired();
            entity.Property(profile => profile.TaxCode).HasMaxLength(50);
            entity.Property(profile => profile.Email).HasMaxLength(250);
            entity.Property(profile => profile.Phone).HasMaxLength(50);
            entity.Property(profile => profile.Address).HasMaxLength(500);
            entity.Property(profile => profile.Website).HasMaxLength(250);
            entity.Property(profile => profile.LogoUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<SystemPreference>(entity =>
        {
            entity.ToTable("SystemPreferences");

            entity.Property(preference => preference.Currency).HasMaxLength(10).IsRequired();
            entity.Property(preference => preference.DateFormat).HasMaxLength(50).IsRequired();
            entity.Property(preference => preference.TimeZone).HasMaxLength(100).IsRequired();
            entity.Property(preference => preference.InvoicePrefix).HasMaxLength(20).IsRequired();
            entity.Property(preference => preference.DefaultTaxRate).HasPrecision(5, 2);
        });
    }

    private void SetAuditFields()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
