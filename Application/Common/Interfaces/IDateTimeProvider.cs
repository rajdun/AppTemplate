namespace Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTimeOffset OffsetNow { get; }
}