using Domain.Common;

namespace Domain.DomainNotifications.User;

public record UserDeactivated(Guid UserId) : IDomainNotification;
