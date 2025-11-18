using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Application.Users.NotificationHandlers;
using Domain.DomainNotifications.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Users.EventHandlers;

public class UserRegisteredIndexNotificationHandlerTests
{
    private readonly ILogger<UserRegisteredSendEmailNotificationHandler> _logger;
    private readonly IElasticSearchService<ElasticUser> _elasticSearchService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserRegisteredIndexNotificationHandler _handler;

    public UserRegisteredIndexNotificationHandlerTests()
    {
        _logger = Substitute.For<ILogger<UserRegisteredSendEmailNotificationHandler>>();
        _elasticSearchService = Substitute.For<IElasticSearchService<ElasticUser>>();
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        _handler = new UserRegisteredIndexNotificationHandler(_logger, _elasticSearchService, _userManager);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowArgumentNullException()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        _userManager.FindByNameAsync(domainEvent.Name).Returns((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(domainEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIndexingFails_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        
        _userManager.FindByNameAsync(domainEvent.Name).Returns(user);
        _elasticSearchService.IndexDocumentAsync(Arg.Any<ElasticUser>())
            .Returns(Task.FromResult(false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(domainEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIndexingSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        
        _userManager.FindByNameAsync(domainEvent.Name).Returns(user);
        _elasticSearchService.IndexDocumentAsync(Arg.Any<ElasticUser>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _elasticSearchService.Received(1).IndexDocumentAsync(
            Arg.Is<ElasticUser>(u => 
                u.Name == domainEvent.Name && 
                u.Email == domainEvent.Email &&
                u.Id == user.Id.ToString()));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldLogError()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        _userManager.FindByNameAsync(domainEvent.Name).Returns((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(domainEvent, CancellationToken.None));
        
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(domainEvent.Email)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenIndexingFails_ShouldLogError()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        
        _userManager.FindByNameAsync(domainEvent.Name).Returns(user);
        _elasticSearchService.IndexDocumentAsync(Arg.Any<ElasticUser>())
            .Returns(Task.FromResult(false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(domainEvent, CancellationToken.None));
        
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(domainEvent.Email)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}

