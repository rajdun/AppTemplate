using Application.Common.MediatorPattern;
using Domain.Aggregates.Identity;
using Domain.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InfrastructureTests.Data;

public class DomainEventDispatcherInterceptorTests
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private readonly DomainEventDispatcherInterceptor _interceptor;

    public DomainEventDispatcherInterceptorTests()
    {
        _mediator = Substitute.For<IMediator>();
        _mediator.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FluentResults.Result.Ok());

        var serviceScope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        _serviceProvider = Substitute.For<IServiceProvider>();

        _serviceProvider.GetService(typeof(IMediator)).Returns(_mediator);
        scopeFactory.CreateScope().Returns(serviceScope);
        serviceScope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _interceptor = new DomainEventDispatcherInterceptor(_serviceProvider);
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
    public async Task SavingChangesAsync_WhenEntityHasDomainEvents_ShouldDispatchThem()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await _mediator.Received(1).PublishAsync(
            Arg.Is<Domain.Aggregates.Identity.DomainEvents.UserRegistered>(e => e.Email == "jan@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityHasNoDomainEvents_ShouldNotDispatch()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);
        profile.ClearDomainEvents(); // No events

        // Act
        await context.SaveChangesAsync();

        // Assert
        await _mediator.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldClearDomainEventsAfterDispatching()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WithMultipleEntitiesWithEvents_ShouldDispatchAllEvents()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        var profile1 = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        var profile2 = UserProfile.Create(Guid.NewGuid(), "Anna", "Nowak", "anna@example.com");

        await context.Set<UserProfile>().AddRangeAsync(profile1, profile2);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await _mediator.Received(2).PublishAsync(
            Arg.Any<Domain.Aggregates.Identity.DomainEvents.UserRegistered>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SavingChanges_WhenEntityHasDomainEvents_ShouldDispatchSynchronously()
    {
        // Arrange
        using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        context.Set<UserProfile>().Add(profile);

        // Act
        context.SaveChanges();

        // Assert
        _mediator.Received(1).PublishAsync(
            Arg.Is<Domain.Aggregates.Identity.DomainEvents.UserRegistered>(e => e.Email == "jan@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChangesAsync_WhenNoEntitiesTracked_ShouldNotDispatch()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Act – SaveChanges with empty ChangeTracker exercises the "no entities" branch
        await context.SaveChangesAsync();

        // Assert
        await _mediator.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SavingChanges_WhenNoEntitiesTracked_ShouldNotDispatch()
    {
        // Arrange
        using var context = CreateDbContextWithInterceptor();

        // Act
        context.SaveChanges();

        // Assert
        _mediator.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChangesAsync_EventsAreClearedBeforeDispatch_ToPreventReprocessing()
    {
        // Arrange
        await using var context = CreateDbContextWithInterceptor();
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        await context.Set<UserProfile>().AddAsync(profile);

        var eventsCountDuringDispatch = -1;
        _mediator.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                eventsCountDuringDispatch = profile.DomainEvents.Count;
                return Task.FromResult(FluentResults.Result.Ok());
            });

        // Act
        await context.SaveChangesAsync();

        // Assert – events were cleared before dispatch
        Assert.Equal(0, eventsCountDuringDispatch);
    }

    [Fact]
    public void Constructor_ShouldAcceptServiceProvider()
    {
        // Arrange & Act
        var interceptor = new DomainEventDispatcherInterceptor(_serviceProvider);

        // Assert
        Assert.NotNull(interceptor);
    }
}





