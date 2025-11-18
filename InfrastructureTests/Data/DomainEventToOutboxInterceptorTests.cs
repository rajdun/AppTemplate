using Application.Common;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace InfrastructureTests.Data;

public class DomainEventToOutboxInterceptorTests
{
    private readonly DomainEventToOutboxInterceptor _interceptor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DomainEventToOutboxInterceptorTests()
    {
        _dateTimeProvider = NSubstitute.Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _interceptor = new DomainEventToOutboxInterceptor(_dateTimeProvider);
    }

    [Fact]
    public async Task SavingChangesAsync_WithDomainEvents_ShouldConvertToOutboxMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainNotification { Message = "Test Event" });
        
        context.TestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        Assert.Single(outboxMessages);
        Assert.Contains("TestDomainEvent", outboxMessages[0].EventType);
        Assert.Contains("Test Event", outboxMessages[0].EventPayload);
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WithMultipleDomainEvents_ShouldConvertAllToOutboxMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainNotification { Message = "Event 1" });
        entity.AddDomainEvent(new TestDomainNotification { Message = "Event 2" });
        entity.AddDomainEvent(new TestDomainNotification { Message = "Event 3" });
        
        context.TestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        Assert.Equal(3, outboxMessages.Count);
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WithNoDomainEvents_ShouldNotCreateOutboxMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        
        var entity = new TestEntity();
        context.TestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        Assert.Empty(outboxMessages);
    }

    [Fact]
    public async Task SavingChangesAsync_WithMultipleEntities_ShouldConvertAllDomainEvents()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        
        var entity1 = new TestEntity();
        entity1.AddDomainEvent(new TestDomainNotification { Message = "Entity 1 Event" });
        
        var entity2 = new TestEntity();
        entity2.AddDomainEvent(new TestDomainNotification { Message = "Entity 2 Event" });
        
        context.TestEntities.Add(entity1);
        context.TestEntities.Add(entity2);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        Assert.Equal(2, outboxMessages.Count);
        Assert.Empty(entity1.DomainEvents);
        Assert.Empty(entity2.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldSetOutboxMessageProperties()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainNotification { Message = "Test" });
        
        context.TestEntities.Add(entity);
        var beforeSave = _dateTimeProvider.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessage = await context.OutboxMessages.FirstAsync();
        Assert.NotNull(outboxMessage.EventType);
        Assert.NotNull(outboxMessage.EventPayload);
        Assert.True(outboxMessage.CreatedAt >= beforeSave);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(0, outboxMessage.RetryCount);
    }

    [Fact]
    public void SavingChanges_WithDomainEvents_ShouldConvertToOutboxMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        using var context = new TestDbContext(options);
        
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainNotification { Message = "Test Event" });
        
        context.TestEntities.Add(entity);

        // Act
        context.SaveChanges();

        // Assert
        var outboxMessages = context.OutboxMessages.ToList();
        Assert.Single(outboxMessages);
        Assert.Empty(entity.DomainEvents);
    }

    private class TestDbContext : DbContext, IApplicationDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        public System.Data.Common.DbConnection GetConnection() => Database.GetDbConnection();
        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync() => 
            Database.BeginTransactionAsync();
        public Task CommitTransactionAsync() => Database.CommitTransactionAsync();
        public Task RollbackTransactionAsync() => Database.RollbackTransactionAsync();
        public IQueryable<T> Query<T>(FormattableString query) => Database.SqlQuery<T>(query);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<TestEntity>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Name);
            });

            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(x => x.EventPayload);
            });
        }
    }

    private class TestEntity : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        
        private readonly List<IDomainNotification> _domainEvents = new();
        public IReadOnlyCollection<IDomainNotification> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainNotification domainNotification)
        {
            _domainEvents.Add(domainNotification);
        }

        public void RemoveDomainEvent(IDomainNotification notificationItem)
        {
            _domainEvents.Remove(notificationItem);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    private class TestDomainNotification : IDomainNotification
    {
        public string Message { get; set; } = string.Empty;
    }
}

