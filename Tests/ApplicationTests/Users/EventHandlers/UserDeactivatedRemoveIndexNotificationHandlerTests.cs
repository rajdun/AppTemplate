using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Application.Users.NotificationHandlers;
using Domain.DomainNotifications.User;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Users.EventHandlers;

public class UserDeactivatedRemoveIndexNotificationHandlerTests
{
    private readonly IElasticSearchService<ElasticUser> _elasticSearchService;
    private readonly ILogger<UserDeactivatedRemoveIndexNotificationHandler> _logger;
    private readonly UserDeactivatedRemoveIndexNotificationHandler _handler;

    public UserDeactivatedRemoveIndexNotificationHandlerTests()
    {
        _elasticSearchService = Substitute.For<IElasticSearchService<ElasticUser>>();
        _logger = Substitute.For<ILogger<UserDeactivatedRemoveIndexNotificationHandler>>();
        _handler = new UserDeactivatedRemoveIndexNotificationHandler(_elasticSearchService, _logger);
    }

    [Fact]
    public async Task Handle_WhenDeletionSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new UserDeactivated(userId);
        
        _elasticSearchService.DeleteDocumentAsync(userId.ToString())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _elasticSearchService.Received(1).DeleteDocumentAsync(userId.ToString());
    }

    [Fact]
    public async Task Handle_WhenDeletionFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new UserDeactivated(userId);
        
        _elasticSearchService.DeleteDocumentAsync(userId.ToString())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == "Could not remove user from index");
    }

    [Fact]
    public async Task Handle_WhenDeletionFails_ShouldLogError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new UserDeactivated(userId);
        
        _elasticSearchService.DeleteDocumentAsync(userId.ToString())
            .Returns(Task.FromResult(false));

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(userId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}

