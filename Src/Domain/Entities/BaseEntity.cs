using Domain.Common;

namespace Domain.Entities;

public abstract class BaseEntity : IEntity
{
    private readonly List<IDomainNotification> _domainNotifications = new();
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainNotification> DomainNotifications => _domainNotifications.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; set; }


    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    public void AddDomainNotification(IDomainNotification domainNotification)
    {
        _domainNotifications.Add(domainNotification);
    }

    public void ClearDomainNotifications()
    {
        _domainNotifications.Clear();
    }
}

public interface IEntity
{
    Guid Id { get; set; }
    public IReadOnlyCollection<IDomainNotification> DomainNotifications { get; }
    public void AddDomainNotification(IDomainNotification domainNotification);
    public void ClearDomainNotifications();
}