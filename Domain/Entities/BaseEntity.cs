using Domain.Common;

namespace Domain.Entities;

public abstract class BaseEntity : IEntity
{
    private readonly List<IDomainNotification> _domainEvents = new();
    public IReadOnlyCollection<IDomainNotification> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; set; }


    public void AddDomainEvent(IDomainNotification domainNotification)
    {
        _domainEvents.Add(domainNotification);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public interface IEntity
{
    Guid Id { get; set; }
    public IReadOnlyCollection<IDomainNotification> DomainEvents { get; }
    public void AddDomainEvent(IDomainNotification domainNotification);
    public void ClearDomainEvents();
}