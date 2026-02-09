using Domain.Common.Interfaces;

namespace Domain.Aggregates.Identity.DomainEvents;

public record UserRegistered(Guid Id, string Name, string Email, string Language) : IDomainEvent;
