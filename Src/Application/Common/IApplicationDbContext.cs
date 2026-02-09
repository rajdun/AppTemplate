using System.Data.Common;
using Domain.Aggregates.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common;

public interface IApplicationDbContext
{
    public DbSet<UserProfile> Profiles { get; set; }

    public DbSet<T> GetSet<T>() where T : class;
    public DbConnection GetConnection();
    public Task<IDbContextTransaction> BeginTransactionAsync();
    public Task CommitTransactionAsync();
    public Task RollbackTransactionAsync();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    public IQueryable<T> Query<T>(FormattableString query);
}
