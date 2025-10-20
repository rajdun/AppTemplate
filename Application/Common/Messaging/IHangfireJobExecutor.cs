namespace Application.Common.Messaging;

public interface IHangfireJobExecutor
{
    Task ProcessEventAsync(string eventType, string eventPayload, CancellationToken cancellationToken);
}