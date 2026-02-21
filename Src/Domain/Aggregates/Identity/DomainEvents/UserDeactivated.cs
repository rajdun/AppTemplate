using Domain.Common.Interfaces;

namespace Domain.Aggregates.Identity.DomainEvents;

public record UserDeactivated(Guid ProfileId) : IDomainEvent;
