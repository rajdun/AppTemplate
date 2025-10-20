using System.Data.Common;
using System.Runtime.CompilerServices;
using Application.Common;
using Domain.Entities;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    protected ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.Id).HasDefaultValueSql("uuidv7()");
        });

        builder.Entity<OutboxMessage>(b =>
        {
            b.Property(x=>x.Id).HasDefaultValueSql("uuidv7()");
            b.Property(x=>x.EventPayload).HasColumnType("jsonb");
            b.HasIndex(x=>x.NextAttemptAt);
        });
    }
    
    public DbConnection GetConnection()
    {
        return Database.GetDbConnection();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await Database.RollbackTransactionAsync();
    }

    public IQueryable<T> Query<T>(FormattableString query)
    {
        return Database.SqlQuery<T>(query);
    }
}