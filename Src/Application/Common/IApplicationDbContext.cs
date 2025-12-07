using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common;

public interface IApplicationDbContext
{
    public DbConnection GetConnection();
    public Task<IDbContextTransaction> BeginTransactionAsync();
    public Task CommitTransactionAsync();
    public Task RollbackTransactionAsync();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    public IQueryable<T> Query<T>(FormattableString query);
}
