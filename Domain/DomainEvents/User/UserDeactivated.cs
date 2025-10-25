using Domain.Common;

namespace Domain.DomainEvents.User;

public record UserDeactivated(Guid UserId) : IDomainEvent;