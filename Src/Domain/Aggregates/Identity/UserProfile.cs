using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common.Models;

namespace Domain.Aggregates.Identity;

public class UserProfile : AggregateRoot<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool CanLogin()
    {
        return !ArchivedAt.HasValue;
    }
}
