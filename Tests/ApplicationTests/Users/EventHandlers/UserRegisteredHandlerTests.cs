using Application.Common;
using Application.Users.EventHandlers;
using Domain.Aggregates.Identity.DomainEvents;
using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common.Interfaces;
using FluentResults;
using NSubstitute;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserRegistered;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserRegistered;

namespace ApplicationTests.Users.EventHandlers;

public class UserRegisteredHandlerTests
{
    private readonly IApplicationDbContext _context;

    public UserRegisteredHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _context.AddDomainNotification(Arg.Any<IDomainNotification>()).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldAddDomainNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var domainEvent = new DomainEvent(profileId, "Jan Kowalski", "jan@example.com", "pl");
        var handler = new UserRegisteredHandler(_context);

        // Act
        var result = await handler.Handle(domainEvent);

        // Assert
        Assert.True(result.IsSuccess);
        await _context.Received(1).AddDomainNotification(
            Arg.Is<DomainNotification>(n =>
                n.Id == profileId &&
                n.Name == "Jan Kowalski" &&
                n.Email == "jan@example.com" &&
                n.Language == "pl"));
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new UserRegisteredHandler(_context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));
    }
}

