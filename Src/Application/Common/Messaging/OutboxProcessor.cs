using Application.Common.Interfaces;
using Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// Assuming IApplicationDbContext is here
// Assuming IMediator is here
// Assuming OutboxMessage is here

// Use built-in System.Text.Json

namespace Application.Common.Messaging;

public class OutboxProcessor(
    ILogger<OutboxProcessor> logger,
    IApplicationDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    IDateTimeProvider dateTimeProvider) : IOutboxProcessor
{
    private const string RawSql = @"
        SELECT * FROM ""Messaging"".""OutboxMessages""
        WHERE ""ProcessedAt"" IS NULL
        AND (""NextAttemptAt"" IS NULL OR ""NextAttemptAt"" <= NOW())
        ORDER BY ""CreatedAt""
        LIMIT 20
        FOR UPDATE SKIP LOCKED";

    private static readonly Action<ILogger, Exception?> _logProcessingOutbox =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] Processing pending outbox messages");

    private static readonly Action<ILogger, Exception?> _logDatabaseError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] Database error while fetching outbox messages. Will retry on next run.");

    private static readonly Action<ILogger, Exception?> _logNoMessages =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] No new outbox messages to process");

    private static readonly Action<ILogger, Guid, string, Exception?> _logEnqueuing =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(4, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] Enqueuing event {EventId} ({EventType}) to Hangfire");

    private static readonly Action<ILogger, Guid, Exception?> _logEnqueueError =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(5, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] Failed to enqueue message {MessageId} to Hangfire. Will retry on next run.");

    private static readonly Action<ILogger, int, Exception?> _logFinished =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(6, nameof(ProcessOutboxMessagesAsync)),
            "[OutboxProcessor] Finished processing batch of {Count} outbox messages");

    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        _logProcessingOutbox(logger, null);

        List<OutboxMessage> messages;
        try
        {
            messages = await dbContext.OutboxMessages
                .FromSqlRaw(RawSql)
                .AsTracking()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Do not catch general exception types - Infrastructure code needs to handle all errors
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            _logDatabaseError(logger, ex);
            return;
        }

        if (messages.Count == 0)
        {
            _logNoMessages(logger, null);
            return;
        }

        var utcNow = dateTimeProvider.UtcNow;

        foreach (var message in messages)
        {
            try
            {
                _logEnqueuing(logger, message.Id, message.EventType, null);

                backgroundJobClient.Enqueue<IHangfireJobExecutor>(executor =>
                    executor.ProcessEventAsync(message.EventType, message.EventPayload, CancellationToken.None)
                );

                message.ProcessedAt = utcNow;
                message.Error = null;
            }
#pragma warning disable CA1031 // Do not catch general exception types - Infrastructure code needs to handle all errors
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logEnqueueError(logger, message.Id, ex);
                message.Error = $"Failed to enqueue: {ex.Message}";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logFinished(logger, messages.Count, null);
    }
}

