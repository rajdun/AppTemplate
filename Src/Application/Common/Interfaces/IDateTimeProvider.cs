namespace Application.Common.Interfaces;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
    public DateTimeOffset OffsetNow { get; }
}
