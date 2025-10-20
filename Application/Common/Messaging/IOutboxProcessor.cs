namespace Application.Common.Messaging;

public interface IOutboxProcessor
{
    Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
}