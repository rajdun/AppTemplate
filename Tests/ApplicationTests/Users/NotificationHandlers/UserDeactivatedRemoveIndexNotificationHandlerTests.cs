using Application.Common.Search;
using Application.Common.Search.Dto.User;
using Application.Users.NotificationHandlers;
using Domain.Aggregates.Identity.DomainNotifications;
using NSubstitute;

namespace ApplicationTests.Users.NotificationHandlers;

public class UserDeactivatedRemoveIndexNotificationHandlerTests
{
    private readonly ISearch<UserSearchDocumentDto> _search;

    public UserDeactivatedRemoveIndexNotificationHandlerTests()
    {
        _search = Substitute.For<ISearch<UserSearchDocumentDto>>();
    }

    [Fact]
    public async Task Handle_WithValidNotification_ShouldDeleteDocumentFromIndex()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new UserDeactivated(userId);
        var handler = new UserDeactivatedRemoveIndexNotificationHandler(_search);

        // Act
        var result = await handler.Handle(notification);

        // Assert
        Assert.True(result.IsSuccess);
        await _search.Received(1).DeleteAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new UserDeactivatedRemoveIndexNotificationHandler(_search);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));
    }
}

