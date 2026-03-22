using Application.Common;
using Application.Users.EventHandlers;
using Domain.Common.Interfaces;
using FluentResults;
using NSubstitute;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserDeactivated;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserDeactivated;

namespace ApplicationTests.Users.EventHandlers;

public class UserDeactivatedHandlerTests
{
    private readonly IApplicationDbContext _context;

    public UserDeactivatedHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _context.AddDomainNotification(Arg.Any<IDomainNotification>()).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldAddDomainNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var domainEvent = new DomainEvent(profileId);
        var handler = new UserDeactivatedHandler(_context);

        // Act
        var result = await handler.Handle(domainEvent);

        // Assert
        Assert.True(result.IsSuccess);
        await _context.Received(1).AddDomainNotification(
            Arg.Is<DomainNotification>(n => n.UserId == profileId));
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new UserDeactivatedHandler(_context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));
    }
}

