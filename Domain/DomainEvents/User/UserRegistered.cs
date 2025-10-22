using Domain.Common;

namespace Domain.DomainEvents.User;

public record UserRegistered(string Name, string Email, string Language) : IDomainEvent;