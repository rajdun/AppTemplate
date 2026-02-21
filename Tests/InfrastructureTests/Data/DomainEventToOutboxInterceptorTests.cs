using Application.Common.Interfaces;
using Domain.Aggregates.Identity;
using Domain.Aggregates.Identity.DomainNotifications;
using Infrastructure.Data;
using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace InfrastructureTests.Data;

public class DomainEventToOutboxInterceptorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DateTime _utcNow = new(2026, 2, 21, 12, 0, 0, DateTimeKind.Utc);
    private readonly DomainEventToOutboxInterceptor _interceptor;

    public DomainEventToOutboxInterceptorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _interceptor = new DomainEventToOutboxInterceptor(_dateTimeProvider);
    }

    private ApplicationDbContext CreateDbContextWithInterceptor()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityHasDomainNotifications_ShouldCreateOutboxMessages()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();
        profile.AddDomainNotification(new UserRegistered(profile.Id, "Jan Kowalski", "jan@example.com", "pl"));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();
        Assert.Single(outboxMessages);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityHasNoDomainNotifications_ShouldNotCreateOutboxMessages()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();
        // No notifications added

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();
        Assert.Empty(outboxMessages);
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldSetCorrectOutboxMessageFields()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profileId = Guid.NewGuid();
        var profile = UserProfile.Create(profileId, "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();

        var notification = new UserRegistered(profileId, "Jan Kowalski", "jan@example.com", "pl");
        profile.AddDomainNotification(notification);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessage = await context.Set<OutboxMessage>().FirstOrDefaultAsync();
        Assert.NotNull(outboxMessage);
        Assert.Equal(typeof(UserRegistered).AssemblyQualifiedName, outboxMessage.EventType);
        Assert.Equal(_utcNow, outboxMessage.CreatedAt);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(0, outboxMessage.RetryCount);
        Assert.Contains("jan@example.com", outboxMessage.EventPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldClearDomainNotificationsAfterProcessing()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();
        profile.AddDomainNotification(new UserRegistered(profile.Id, "Jan Kowalski", "jan@example.com", "pl"));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(profile.DomainNotifications);
    }

    [Fact]
    public async Task SavingChangesAsync_WithMultipleEntitiesAndNotifications_ShouldCreateOneOutboxMessagePerNotification()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        var profile1 = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        var profile2 = UserProfile.Create(Guid.NewGuid(), "Anna", "Nowak", "anna@example.com");
        await context.Set<UserProfile>().AddRangeAsync(profile1, profile2);

        profile1.ClearDomainEvents();
        profile2.ClearDomainEvents();

        profile1.AddDomainNotification(new UserRegistered(profile1.Id, "Jan Kowalski", "jan@example.com", "pl"));
        profile2.AddDomainNotification(new UserRegistered(profile2.Id, "Anna Nowak", "anna@example.com", "en"));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();
        Assert.Equal(2, outboxMessages.Count);
    }

    [Fact]
    public async Task SavingChangesAsync_OutboxMessagePayload_ShouldBeSerializedJson()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profileId = Guid.NewGuid();
        var profile = UserProfile.Create(profileId, "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();
        profile.AddDomainNotification(new UserRegistered(profileId, "Jan Kowalski", "jan@example.com", "pl"));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessage = await context.Set<OutboxMessage>().FirstAsync();
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<UserRegistered>(outboxMessage.EventPayload);
        Assert.NotNull(deserialized);
        Assert.Equal("Jan Kowalski", deserialized.Name);
        Assert.Equal("jan@example.com", deserialized.Email);
        Assert.Equal(profileId, deserialized.Id);
        Assert.Equal("pl", deserialized.Language);
    }

    [Fact]
    public void SavingChanges_WhenEntityHasDomainNotifications_ShouldCreateOutboxMessages()
    {
        // Arrange
        using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        context.Set<UserProfile>().Add(profile);
        profile.ClearDomainEvents();
        profile.AddDomainNotification(new UserRegistered(profile.Id, "Jan Kowalski", "jan@example.com", "pl"));

        // Act
        context.SaveChanges();

        // Assert
        var outboxMessages = context.Set<OutboxMessage>().ToList();
        Assert.Single(outboxMessages);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenNoEntitiesTracked_ShouldNotCreateOutboxMessages()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Act – empty ChangeTracker exercises the "no entities" branch
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();
        Assert.Empty(outboxMessages);
    }

    [Fact]
    public void SavingChanges_WhenNoEntitiesTracked_ShouldNotCreateOutboxMessages()
    {
        // Arrange
        using var context = CreateDbContextWithInterceptor();

        // Act
        context.SaveChanges();

        // Assert
        var outboxMessages = context.Set<OutboxMessage>().ToList();
        Assert.Empty(outboxMessages);
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldUseDateTimeProviderForCreatedAt()
    {
        // Arrange
        var specificTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(specificTime);

        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents();
        profile.AddDomainNotification(new UserRegistered(profile.Id, "Jan Kowalski", "jan@example.com", "pl"));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessage = await context.Set<OutboxMessage>().FirstAsync();
        Assert.Equal(specificTime, outboxMessage.CreatedAt);
    }

    [Fact]
    public void Constructor_ShouldAcceptDateTimeProvider()
    {
        // Arrange & Act
        var interceptor = new DomainEventToOutboxInterceptor(_dateTimeProvider);

        // Assert
        Assert.NotNull(interceptor);
    }
}




