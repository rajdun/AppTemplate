using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.DomainEvents.User;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Users;

[Table("Users", Schema = "Users")]
public class ApplicationUser : IdentityUser<Guid>, IEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static ApplicationUser Create(string userName, string? email)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName
        };

        if (!string.IsNullOrWhiteSpace(email)) user.AddDomainEvent(new UserRegistered(userName, email));

        return user;
    }
}

public class ApplicationRole : IdentityRole<Guid>
{
}