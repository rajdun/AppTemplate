using Application.Common.Interfaces;

namespace Infrastructure.Implementation;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; } = DateTime.UtcNow;
    public DateTimeOffset OffsetNow { get; } = DateTimeOffset.UtcNow;
}