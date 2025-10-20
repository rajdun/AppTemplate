using Application.Common.Mediator;
using Application.Common.Messaging;
using Domain.Common;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public class HangfireJobExecutor(
    ILogger<HangfireJobExecutor> logger,
    IMediator mediator,
    IDomainEventDeserializer domainEventDeserializer) : IHangfireJobExecutor
{
    public async Task ProcessEventAsync(string eventType, string eventPayload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Hangfire job started for event type {EventType}", eventType);

        dynamic? domainEvent = domainEventDeserializer.Deserialize(eventType, eventPayload);

        if (domainEvent is null)
        {
            logger.LogWarning("Failed to deserialize event of type {EventType}. The event might be unknown or the payload invalid. Hangfire will retry.", eventType);
            throw new InvalidOperationException($"Could not deserialize event of type '{eventType}'.");
        }
        
        var result = await mediator.PublishAsync(domainEvent, cancellationToken) as Result;
        
        if (result != null && !result.IsSuccess)
        {
            logger.LogWarning("Processing of event type {EventType} failed. See logs for details. Hangfire will retry.", eventType);
            throw new InvalidOperationException($"Processing failed for event type {eventType}. See logs for details.");
        }
        
        logger.LogInformation("Hangfire job successfully completed for event type {EventType}", eventType);
    }
}