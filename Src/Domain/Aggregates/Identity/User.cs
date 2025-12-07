using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common.Models;

namespace Domain.Aggregates.Identity;

public class User : AggregateRoot<Guid>
{
    private DateTimeOffset? _deactivatedAt;

    public string UserName { get; private set; }
    public string? Email { get; private set; }

    public DateTimeOffset? DeactivatedAt
    {
        get => _deactivatedAt;
        set
        {
            if (_deactivatedAt == null && value != null)
            {
                AddDomainNotification(new UserDeactivated(Id));
            }
            _deactivatedAt = value;
        }
    }

    public User(string userName, string? email, string language = "pl")
    {
        UserName = userName;
        Email = email;

        if (!string.IsNullOrWhiteSpace(email))
        {
            AddDomainNotification(new UserRegistered(userName, email, language));
        }
    }
}
