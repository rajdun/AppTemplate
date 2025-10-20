using Domain.Common;

namespace Domain.Entities;

public abstract class BaseEntity : IEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
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
}

public interface IEntity
{
    Guid Id { get; set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    public void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}