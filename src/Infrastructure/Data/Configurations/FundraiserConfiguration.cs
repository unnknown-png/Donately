using Donately.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Donately.Infrastructure.Data.Configurations;

public class FundraiserConfiguration : IEntityTypeConfiguration<Fundraiser>
{
    public void Configure(EntityTypeBuilder<Fundraiser> builder)
    {
        builder.ToTable("Fundraisers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasColumnType("text");

        builder.Property(x => x.GoalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.CurrentAmount)
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("UAH")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnType("bytea")
            .IsConcurrencyToken();

        builder.HasQueryFilter(x => !x.IsDeleted);


        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Donations)
            .WithOne(x => x.Fundraiser)
            .HasForeignKey(x => x.FundraiserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

