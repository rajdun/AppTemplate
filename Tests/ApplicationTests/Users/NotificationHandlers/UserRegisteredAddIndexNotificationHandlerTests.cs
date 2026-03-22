using Application.Common.Search;
using Application.Common.Search.Dto.User;
using Application.Users.NotificationHandlers;
using Domain.Aggregates.Identity.DomainNotifications;
using NSubstitute;

namespace ApplicationTests.Users.NotificationHandlers;

public class UserRegisteredAddIndexNotificationHandlerTests
{
    private readonly ISearch<UserSearchDocumentDto> _search;

    public UserRegisteredAddIndexNotificationHandlerTests()
    {
        _search = Substitute.For<ISearch<UserSearchDocumentDto>>();
    }

    [Fact]
    public async Task Handle_WithValidNotification_ShouldIndexDocument()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new UserRegistered(userId, "Jan Kowalski", "jan@example.com", "pl");
        var handler = new UserRegisteredAddIndexNotificationHandler(_search);

        // Act
        var result = await handler.Handle(notification);

        // Assert
        Assert.True(result.IsSuccess);
        await _search.Received(1).IndexAsync(
            Arg.Is<IEnumerable<UserSearchDocumentDto>>(docs =>
                docs.Any(d => d.Id == userId &&
                              d.Name == "Jan Kowalski" &&
                              d.Email == "jan@example.com")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new UserRegisteredAddIndexNotificationHandler(_search);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));
    }
}

