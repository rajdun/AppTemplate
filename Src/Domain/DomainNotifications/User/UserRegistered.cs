using Domain.Common;

namespace Domain.DomainNotifications.User;

public record UserRegistered(string Name, string Email, string Language) : IDomainNotification;