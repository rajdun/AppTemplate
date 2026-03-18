using System.Data.Common;
using System.Text.Json;
using Application.Common;
using Domain.Common.Interfaces;
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

    public DbSet<T> GetSet<T>() where T : class
    {
        return Set<T>();
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

        builder.UseCollation("pl-PL-x-icu");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public async Task<long> GetApproxRowCountAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var tableName = GetSet<T>().EntityType.GetTableName();

        var approxCount = await Query<long>($"SELECT reltuples::bigint AS \"Value\" FROM pg_class WHERE relname = {tableName}")
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (approxCount < 0)
        {
            approxCount = await GetSet<T>()
                .LongCountAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return approxCount;
    }

    public async Task AddDomainNotification(IDomainNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var message = new OutboxMessage
        {
            EventType = notification.GetType().AssemblyQualifiedName!,
            EventPayload = JsonSerializer.Serialize(notification, notification.GetType()),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            RetryCount = 0
        };

        await OutboxMessages.AddAsync(message).ConfigureAwait(false);
    }
}
