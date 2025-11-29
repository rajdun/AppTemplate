using Application.Common.MediatorPattern;
using Application.Common.Messaging;
using Domain.Common;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public partial class HangfireJobExecutor(
    ILogger<HangfireJobExecutor> logger,
    IMediator mediator,
    IDomainNotificationDeserializer domainNotificationDeserializer) : IHangfireJobExecutor
{
    [LoggerMessage(LogLevel.Warning, "Failed to deserialize event of type {EventType}. The event might be unknown or the payload invalid. Hangfire will retry.")]
    private static partial void LogDeserializationWarning(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Warning, "Processing of event type {EventType} failed. See logs for details. Hangfire will retry.")]
    private static partial void LogProcessingFailureWarning(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Information, "Hangfire job started for event type {EventType}")]
    private static partial void LogJobStartedInformation(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Information, "Hangfire job successfully completed for event type {EventType}")]
    private static partial void LogJobCompletedInformation(ILogger logger, string eventType);

    public async Task ProcessEventAsync(string eventType, string eventPayload, CancellationToken cancellationToken)
    {
        LogJobStartedInformation(logger, eventType);

        var domainEvent = domainNotificationDeserializer.Deserialize(eventType, eventPayload);

        if (domainEvent is null)
        {
            LogDeserializationWarning(logger, eventType);
            throw new InvalidOperationException($"Could not deserialize event of type '{eventType}'.");
        }

        var result = await mediator.PublishAsync(domainEvent, cancellationToken) as Result;

        if (result != null && !result.IsSuccess)
        {
            LogProcessingFailureWarning(logger, eventType);
            throw new InvalidOperationException(
                $"Processing of event type '{eventType}' failed: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        }

        LogJobCompletedInformation(logger, eventType);
    }
}
