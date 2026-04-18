using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;

namespace TsLaser.Crm.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Partner> Partners => Set<Partner>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Tattoo> Tattoos => Set<Tattoo>();

    public DbSet<LaserSession> LaserSessions => Set<LaserSession>();

    public DbSet<IntakeSubmission> IntakeSubmissions => Set<IntakeSubmission>();

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partner>(entity =>
        {
            entity.ToTable("partners");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Name).HasDatabaseName("ix_partners_name");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.BirthDate).HasColumnName("birth_date");
            entity.Property(x => x.ReferralPartnerId).HasColumnName("referral_partner_id");
            entity.Property(x => x.ReferralCustom).HasMaxLength(255).HasColumnName("referral_custom");
            entity.Property(x => x.StoppedReason).HasColumnName("stopped_reason");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            entity.HasIndex(x => x.Name).HasDatabaseName("ix_clients_name");
            entity.HasIndex(x => x.Phone).HasDatabaseName("ix_clients_phone");

            entity
                .HasOne(x => x.ReferralPartner)
                .WithMany(x => x.ReferredClients)
                .HasForeignKey(x => x.ReferralPartnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Tattoo>(entity =>
        {
            entity.ToTable("tattoos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientId).HasColumnName("client_id");
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.RemovalZone).HasMaxLength(255).HasColumnName("removal_zone");
            entity.Property(x => x.CorrectionsCount).HasMaxLength(100).HasColumnName("corrections_count");
            entity.Property(x => x.LastPigmentDate).HasColumnName("last_pigment_date");
            entity.Property(x => x.LastLaserDate).HasColumnName("last_laser_date");
            entity.Property(x => x.NoLaserBefore).HasColumnName("no_laser_before");
            entity.Property(x => x.PreviousRemovalPlace).HasMaxLength(255).HasColumnName("previous_removal_place");
            entity.Property(x => x.DesiredResult).HasColumnName("desired_result");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.ClientId).HasDatabaseName("ix_tattoos_client_id");

            entity
                .HasOne(x => x.Client)
                .WithMany(x => x.Tattoos)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaserSession>(entity =>
        {
            entity.ToTable("laser_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientId).HasColumnName("client_id");
            entity.Property(x => x.TattooId).HasColumnName("tattoo_id");
            entity.Property(x => x.TattooName).HasMaxLength(255).HasColumnName("tattoo_name");
            entity.Property(x => x.SessionNumber).HasColumnName("session_number");
            entity.Property(x => x.SubSession).HasMaxLength(10).HasColumnName("sub_session");
            entity.Property(x => x.Wavelength).HasMaxLength(100).HasColumnName("wavelength");
            entity.Property(x => x.Diameter).HasMaxLength(100).HasColumnName("diameter");
            entity.Property(x => x.Density).HasMaxLength(100).HasColumnName("density");
            entity.Property(x => x.Hertz).HasMaxLength(100).HasColumnName("hertz");
            entity.Property(x => x.FlashesCount).HasColumnName("flashes_count");
            entity.Property(x => x.SessionDate).HasColumnName("session_date");
            entity.Property(x => x.BreakPeriod).HasMaxLength(100).HasColumnName("break_period");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.ClientId).HasDatabaseName("ix_laser_sessions_client_id");
            entity.HasIndex(x => x.TattooId).HasDatabaseName("ix_laser_sessions_tattoo_id");

            entity
                .HasOne(x => x.Client)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Tattoo)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.TattooId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IntakeSubmission>(entity =>
        {
            entity.ToTable("intake_submissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientId).HasColumnName("client_id");
            entity.Property(x => x.TattooId).HasColumnName("tattoo_id");
            entity.Property(x => x.ApprovedClientId).HasColumnName("approved_client_id");
            entity.Property(x => x.ApprovedTattooId).HasColumnName("approved_tattoo_id");
            entity.Property(x => x.FullName).HasMaxLength(255).HasColumnName("full_name").IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(50).HasColumnName("gender");
            entity.Property(x => x.Phone).HasMaxLength(20).HasColumnName("phone").IsRequired();
            entity.Property(x => x.BirthDate).HasColumnName("birth_date");
            entity.Property(x => x.ReferralSource).HasMaxLength(255).HasColumnName("referral_source");
            entity.Property(x => x.TattooType).HasMaxLength(255).HasColumnName("tattoo_type");
            entity.Property(x => x.TattooAge).HasMaxLength(255).HasColumnName("tattoo_age");
            entity.Property(x => x.CorrectionsInfo).HasColumnName("corrections_info");
            entity.Property(x => x.PreviousRemovalInfo).HasColumnName("previous_removal_info");
            entity.Property(x => x.PreviousRemovalWhere).HasColumnName("previous_removal_where");
            entity.Property(x => x.DesiredResult).HasColumnName("desired_result");
            entity.Property(x => x.Status).HasMaxLength(20).HasColumnName("status").HasDefaultValue(IntakeSubmissionStatus.Pending);
            entity.Property(x => x.IsNewClient).HasColumnName("is_new_client");
            entity.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(x => x.ReviewedBy).HasMaxLength(100).HasColumnName("reviewed_by");
            entity.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(x => x.RawPayload).HasColumnName("raw_payload");
            entity.Property(x => x.Source).HasMaxLength(50).HasColumnName("source").HasDefaultValue("landing");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.ClientId).HasDatabaseName("ix_intake_submissions_client_id");
            entity.HasIndex(x => x.TattooId).HasDatabaseName("ix_intake_submissions_tattoo_id");
            entity.HasIndex(x => x.Phone).HasDatabaseName("ix_intake_submissions_phone");
            entity.HasIndex(x => x.Status).HasDatabaseName("ix_intake_submissions_status");
            entity.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_intake_submissions_created_at");
            entity.HasIndex(x => x.ApprovedClientId).HasDatabaseName("ix_intake_submissions_approved_client_id");
            entity.HasIndex(x => x.ApprovedTattooId).HasDatabaseName("ix_intake_submissions_approved_tattoo_id");

            entity
                .HasOne(x => x.Client)
                .WithMany(x => x.IntakeSubmissions)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.Tattoo)
                .WithMany(x => x.IntakeSubmissions)
                .HasForeignKey(x => x.TattooId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ApplyTimestamps()
    {
        var entries = ChangeTracker
            .Entries<TimestampedEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
