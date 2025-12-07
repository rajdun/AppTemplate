using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Application.Users.NotificationHandlers;
using Domain.Aggregates.Identity;
using Domain.Aggregates.Identity.DomainNotifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Users.EventHandlers;

public class UserRegisteredIndexNotificationHandlerTests
{
    private readonly ISearch<UserSearchDocumentDto> _search;
    private readonly UserManager<User> _userManager;
    private readonly UserRegisteredIndexNotificationHandler _handler;

    public UserRegisteredIndexNotificationHandlerTests()
    {
        var logger = Substitute.For<ILogger<UserRegisteredIndexNotificationHandler>>();
        _search = Substitute.For<ISearch<UserSearchDocumentDto>>();
        var userStore = Substitute.For<IUserStore<User>>();
        _userManager = Substitute.For<UserManager<User>>(
            userStore, null, null, null, null, null, null, null, null);
        _handler = new UserRegisteredIndexNotificationHandler(logger, _search, _userManager);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        _userManager.FindByNameAsync(domainEvent.Name).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(domainEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIndexingFails_ShouldThrowException()
    {
        // Arrange
        var user = new User("testuser", "test@test.com", "en");
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");

        _userManager.FindByNameAsync(domainEvent.Name).Returns(user);
        _search.IndexAsync(Arg.Any<IEnumerable<UserSearchDocumentDto>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Index failed")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(domainEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIndexingSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var user = new User("testuser", "test@test.com", "en");
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");

        _userManager.FindByNameAsync(domainEvent.Name).Returns(user);
        _search.IndexAsync(Arg.Any<IEnumerable<UserSearchDocumentDto>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _search.Received(1).IndexAsync(
            Arg.Is<IEnumerable<UserSearchDocumentDto>>(docs =>
                docs.Any(u =>
                    u.Name == domainEvent.Name &&
                    u.Email == domainEvent.Email &&
                    u.Id == user.Id)),
            Arg.Any<CancellationToken>());
    }
}

