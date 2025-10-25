using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Domain.Common;
using Domain.DomainEvents.User;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Users;

[Table("Users", Schema = "Users")]
public class ApplicationUser : IdentityUser<Guid>, IEntity
{
    #region Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    private DateTimeOffset? _deactivatedAt;

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    #endregion

    public DateTimeOffset? DeactivatedAt
    {
        get => _deactivatedAt;
        set
        {
                        if (_deactivatedAt == null && value != null)
            {
                AddDomainEvent(new UserDeactivated(Id));
            }
            _deactivatedAt = value;
        }
    }

    public static ApplicationUser Create(string userName, string? email, string language = "pl")
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName
        };
        
        if (!string.IsNullOrWhiteSpace(email)) user.AddDomainEvent(new UserRegistered(userName, email, language));

        return user;
    }
}

public class ApplicationRole : IdentityRole<Guid>
{
}