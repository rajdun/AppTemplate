using Domain.Aggregates.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FileMetadata", "Storage");
        builder.ToTable("FileMetadata");
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(256).IsRequired();
        builder.Property(f => f.SizeInBytes).IsRequired();
        builder.Property(f => f.Checksum).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Provider).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(f => f.ProviderKey).HasMaxLength(1024).IsRequired();
        builder.HasIndex(f => f.Checksum);
    }
}
