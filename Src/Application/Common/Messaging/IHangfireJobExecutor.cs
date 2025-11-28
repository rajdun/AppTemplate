namespace Application.Common.Messaging;

public interface IHangfireJobExecutor
{
    public Task ProcessEventAsync(string eventType, string eventPayload, CancellationToken cancellationToken);
}
