using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxMessages", "Messaging");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventPayload).HasColumnType("jsonb");
        builder.HasIndex(x => x.NextAttemptAt);
    }
}
