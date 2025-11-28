namespace Application.Common.Messaging;

public interface IOutboxProcessor
{
    public Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
}
