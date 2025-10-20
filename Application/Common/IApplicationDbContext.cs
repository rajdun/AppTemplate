using System.Data.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common;

public interface IApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set;  }
    
    DbConnection GetConnection();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    IQueryable<T> Query<T>(FormattableString query);
}