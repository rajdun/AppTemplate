using Domain.Aggregates.Identity;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.HasOne(x => x.DomainUserProfile)
            .WithOne()
            .HasForeignKey<UserProfile>(up => up.Id)
            .IsRequired();
    }
}
