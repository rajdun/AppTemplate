using Domain.Aggregates.Licencing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class LicenceConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Licencing.Licence>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Licencing.Licence> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Licences", "Licencing");

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


