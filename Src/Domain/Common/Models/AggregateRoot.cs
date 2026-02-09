using Domain.Common.Interfaces;

namespace Domain.Common.Models;

public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    private readonly List<IDomainNotification> _domainNotifications = new();
    public IReadOnlyCollection<IDomainNotification> DomainNotifications => _domainNotifications.AsReadOnly();

    public void AddDomainNotification(IDomainNotification domainNotification) =>
        _domainNotifications.Add(domainNotification);

    public void ClearDomainNotifications() => _domainNotifications.Clear();
}
