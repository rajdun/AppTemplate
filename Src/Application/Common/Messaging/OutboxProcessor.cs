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

    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing outbox messages...");

        List<OutboxMessage> messages;
        try
        {
            messages = await dbContext.OutboxMessages
                .FromSqlRaw(RawSql)
                .AsTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error while fetching outbox messages. Will retry on next run.");
            return;
        }

        if (messages.Count == 0)
        {
            logger.LogDebug("No new outbox messages to process.");
            return;
        }

        var utcNow = dateTimeProvider.UtcNow;

        foreach (var message in messages)
            try
            {
                logger.LogInformation("Enqueuing event {EventId} ({EventType}) to Hangfire", message.Id,
                    message.EventType);

                backgroundJobClient.Enqueue<IHangfireJobExecutor>(executor =>
                    executor.ProcessEventAsync(message.EventType, message.EventPayload, CancellationToken.None)
                );

                message.ProcessedAt = utcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue message {MessageId} to Hangfire. Will retry on next run.",
                    message.Id);
                message.Error = $"Failed to enqueue: {ex.Message}";
            }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Finished processing batch of {Count} outbox messages.", messages.Count);
    }
}