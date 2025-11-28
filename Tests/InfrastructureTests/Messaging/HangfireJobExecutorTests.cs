using Application.Common.Messaging;
using Domain.Common;
using FluentResults;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfrastructureTests.Messaging;

public class HangfireJobExecutorTests
{
    private readonly ILogger<HangfireJobExecutor> _logger;
    private readonly Application.Common.Mediator.IMediator _mediator;
    private readonly IDomainNotificationDeserializer _deserializer;
    private readonly HangfireJobExecutor _sut;

    public HangfireJobExecutorTests()
    {
        _logger = Substitute.For<ILogger<HangfireJobExecutor>>();
        _mediator = Substitute.For<Application.Common.Mediator.IMediator>();
        _deserializer = Substitute.For<IDomainNotificationDeserializer>();
        _sut = new HangfireJobExecutor(_logger, _mediator, _deserializer);
    }

    [Fact]
    public async Task ProcessEventAsync_WithValidEvent_ShouldDeserializeAndPublish()
    {
        // Arrange
        var eventType = "TestEvent";
        var eventPayload = "{\"data\":\"test\"}";
        var domainEvent = Substitute.For<IRequest>();

        _deserializer.Deserialize(eventType, eventPayload)?.Returns(domainEvent);
        _mediator.PublishAsync(domainEvent, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        // Act
        await _sut.ProcessEventAsync(eventType, eventPayload, CancellationToken.None);

        // Assert
        _deserializer.Received(1).Deserialize(eventType, eventPayload);
        await _mediator.Received(1).PublishAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEventAsync_WhenDeserializationFails_ShouldThrowException()
    {
        // Arrange
        var eventType = "UnknownEvent";
        var eventPayload = "{\"data\":\"test\"}";

        _deserializer.Deserialize(eventType, eventPayload)?.Returns((IRequest?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ProcessEventAsync(eventType, eventPayload, CancellationToken.None));

        Assert.Contains("Could not deserialize event of type", exception.Message);
        await _mediator.DidNotReceive().PublishAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEventAsync_WhenPublishFails_ShouldThrowException()
    {
        // Arrange
        var eventType = "TestEvent";
        var eventPayload = "{\"data\":\"test\"}";
        var domainEvent = Substitute.For<IRequest>();
        var failedResult = Result.Fail("Processing failed");

        _deserializer.Deserialize(eventType, eventPayload)?.Returns(domainEvent);
        _mediator.PublishAsync(domainEvent, Arg.Any<CancellationToken>()).Returns(failedResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ProcessEventAsync(eventType, eventPayload, CancellationToken.None));

        Assert.Contains("Processing of event type", exception.Message);
        Assert.Contains("failed", exception.Message);
    }

    [Fact]
    public async Task ProcessEventAsync_WhenPublishSucceeds_ShouldCompleteSuccessfully()
    {
        // Arrange
        var eventType = "TestEvent";
        var eventPayload = "{\"data\":\"test\"}";
        var domainEvent = Substitute.For<IRequest>();

        _deserializer.Deserialize(eventType, eventPayload)!.Returns(domainEvent);
        _mediator.PublishAsync(domainEvent, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        // Act
        await _sut.ProcessEventAsync(eventType, eventPayload, CancellationToken.None);

        // Assert - No exception thrown
        _deserializer.Received(1).Deserialize(eventType, eventPayload);
        await _mediator.Received(1).PublishAsync(domainEvent, Arg.Any<CancellationToken>());
    }
}

