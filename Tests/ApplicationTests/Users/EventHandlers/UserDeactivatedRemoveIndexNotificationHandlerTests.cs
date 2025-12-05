using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Users.NotificationHandlers;
using Domain.DomainNotifications.User;
using NSubstitute;

namespace ApplicationTests.Users.EventHandlers;

public class UserDeactivatedRemoveIndexNotificationHandlerTests
{
    private readonly ISearch<UserSearchDocumentDto> _search;
    private readonly UserDeactivatedRemoveIndexNotificationHandler _handler;

    public UserDeactivatedRemoveIndexNotificationHandlerTests()
    {
        _search = Substitute.For<ISearch<UserSearchDocumentDto>>();
        _handler = new UserDeactivatedRemoveIndexNotificationHandler(_search);
    }

    [Fact]
    public async Task Handle_WhenDeletionSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new UserDeactivated(userId);

        _search.DeleteAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _search.Received(1).DeleteAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDeletionFails_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new UserDeactivated(userId);

        _search.DeleteAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Delete failed")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(domainEvent, CancellationToken.None));
    }
}

