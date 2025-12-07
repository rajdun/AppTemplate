using Domain.Common.Interfaces;

namespace Domain.Aggregates.Identity.DomainNotifications;

public record UserDeactivated(Guid UserId) : IDomainNotification;
