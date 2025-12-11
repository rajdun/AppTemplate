using Application.Common.MediatorPattern;
using Application.Common.Messaging;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public partial class HangfireJobExecutor(
    ILogger<HangfireJobExecutor> logger,
    IMediator mediator,
    IDomainNotificationDeserializer domainNotificationDeserializer) : IHangfireJobExecutor
{
    [LoggerMessage(LogLevel.Warning, "[HangfireJobExecutor] Failed to deserialize event type {EventType}; payload might be unknown or invalid. Hangfire will retry.")]
    private static partial void LogDeserializationWarning(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Warning, "[HangfireJobExecutor] Processing for event type {EventType} failed. See previous logs. Hangfire will retry.")]
    private static partial void LogProcessingFailureWarning(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Information, "[HangfireJobExecutor] Hangfire job started for event type {EventType}")]
    private static partial void LogJobStartedInformation(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Information, "[HangfireJobExecutor] Hangfire job completed successfully for event type {EventType}")]
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
