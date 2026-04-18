using Donately.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fundraiser> Fundraisers => Set<Fundraiser>();

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(x => x.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(x => x.FullName)
                .HasMaxLength(200);

            entity.Property(x => x.IsVerified)
                .HasDefaultValue(false);

            entity.Property(x => x.VerificationStatus)
                .HasConversion<int>()
                .HasDefaultValue(VerificationStatus.None);

            entity.Property(x => x.ProfileImagePath)
                .HasMaxLength(500);

            entity.Property(x => x.Bio)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasIndex(x => x.IsVerified);

            entity.HasIndex(x => x.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("\"NormalizedEmail\" IS NOT NULL");
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}
