using System.Data.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common;

public interface IApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public DbConnection GetConnection();
    public Task<IDbContextTransaction> BeginTransactionAsync();
    public Task CommitTransactionAsync();
    public Task RollbackTransactionAsync();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    public IQueryable<T> Query<T>(FormattableString query);
}
