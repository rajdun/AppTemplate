using Domain.Aggregates.Identity.DomainEvents;
using Domain.Common.Models;

namespace Domain.Aggregates.Identity;

public class UserProfile : AggregateRoot<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTimeOffset? ArchivedAt { get; private set; }

    // Private constructor for EF Core
    private UserProfile()
    {
    }

    private UserProfile(Guid id, string firstName, string lastName, string email)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public static UserProfile Create(Guid id, string firstName, string lastName, string email, string language = "pl")
    {
        var profile = new UserProfile(id, firstName, lastName, email);
        profile.AddDomainEvent(new UserRegistered(id, $"{firstName} {lastName}", email, language));
        return profile;
    }

    public bool CanLogin()
    {
        return !ArchivedAt.HasValue;
    }

    public void Deactivate(DateTimeOffset archivedAt)
    {
        if (ArchivedAt.HasValue)
        {
            return;
        }

        ArchivedAt = archivedAt;
        AddDomainEvent(new UserDeactivated(Id));
    }

    public void Update(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
