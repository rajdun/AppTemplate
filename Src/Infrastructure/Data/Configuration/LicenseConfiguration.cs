using Domain.Aggregates.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Licensing.License>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Licensing.License> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Licenses");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.TenantId)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(l => l.TenantId)
            .IsUnique();

        builder.Property(l => l.RawJwtToken)
            .IsRequired();

        builder.Property(l => l.CompanyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.ExpiresAt)
            .IsRequired();

        builder.Property(l => l.MaxUsers)
            .IsRequired();

        builder.Property(l => l.LastSyncedAt)
            .IsRequired();

        builder.Property<List<string>>("_activeFeatures")
            .HasField("_activeFeatures")
            .HasColumnName("ActiveFeatures")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}


