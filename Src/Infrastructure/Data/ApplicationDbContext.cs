using System.Data.Common;
using Application.Common;
using Domain.Aggregates.Identity;
using Infrastructure.Identity;
using Infrastructure.Messaging.Dto;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
    IApplicationDbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbConnection GetConnection()
    {
        return Database.GetDbConnection();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await Database.BeginTransactionAsync().ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync()
    {
        await Database.CommitTransactionAsync().ConfigureAwait(false);
    }

    public async Task RollbackTransactionAsync()
    {
        await Database.RollbackTransactionAsync().ConfigureAwait(false);
    }

    public IQueryable<T> Query<T>(FormattableString query)
    {
        return Database.SqlQuery<T>(query);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.Id).HasDefaultValueSql("uuidv7()");
            b.HasOne(x => x.DomainUserProfile)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.Id)
                .IsRequired();
        });

        builder.Entity<UserProfile>(b =>
        {
            b.ToTable("UserProfiles");
            b.Property(u => u.Id).HasDefaultValueSql("uuidv7()");
            b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            b.Property(u => u.Email).HasMaxLength(256).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<OutboxMessage>(b =>
        {
            b.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
            b.Property(x => x.EventPayload).HasColumnType("jsonb");
            b.HasIndex(x => x.NextAttemptAt);
        });
    }
}
