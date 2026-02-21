using Domain.Common.Interfaces;

namespace Domain.Aggregates.Identity.DomainEvents;

public record UserRegistered(Guid ProfileId, string Name, string Email, string Language) : IDomainEvent;
