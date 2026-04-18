using Donately.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Donately.Infrastructure.Data.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ProviderEventId)
            .HasMaxLength(200);

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb");

        builder.Property(x => x.Error)
            .HasColumnType("text");

        builder.Property(x => x.ReceivedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.Processed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ReceivedAt);
        builder.HasIndex(x => x.Processed);

        builder.HasIndex(x => new { x.Provider, x.ProviderEventId })
            .IsUnique()
            .HasFilter("\"ProviderEventId\" IS NOT NULL");
    }
}

