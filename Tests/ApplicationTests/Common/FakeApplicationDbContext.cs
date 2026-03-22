using System.Data.Common;
using Application.Common;
using Domain.Aggregates.Identity;
using Domain.Common.Interfaces;
using LicenseEntity = Domain.Aggregates.Licensing.License;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ApplicationTests.Common;

/// <summary>
/// Lightweight in-memory EF Core context that implements IApplicationDbContext
/// for use in Application-layer handler tests.
/// </summary>
internal sealed class FakeApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<UserProfile> Profiles { get; set; } = null!;
    public DbSet<LicenseEntity> Licenses { get; set; } = null!;

    public FakeApplicationDbContext(DbContextOptions<FakeApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LicenseEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.TenantId);
            e.Property(l => l.RawJwtToken);
            e.Property(l => l.CompanyName);
            e.Property(l => l.ExpiresAt);
            e.Property(l => l.MaxUsers);
            e.Property(l => l.LastSyncedAt);
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FirstName);
            e.Property(p => p.LastName);
            e.Property(p => p.Email);
            e.Property(p => p.ArchivedAt);
            e.Ignore(p => p.DomainEvents);
            e.Ignore(p => p.DomainNotifications);
        });
    }

    public DbSet<T> GetSet<T>() where T : class => Set<T>();

    public DbConnection GetConnection() => throw new NotSupportedException();

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    public IQueryable<T> Query<T>(FormattableString query) => throw new NotSupportedException();

    public Task<long> GetApproxRowCountAsync<T>(CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException();

    public Task AddDomainNotification(IDomainNotification notification)
    {
        // Track notification in a simple list for assertion purposes
        DomainNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public List<IDomainNotification> DomainNotifications { get; } = new();

    public static FakeApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FakeApplicationDbContext(options);
    }
}
