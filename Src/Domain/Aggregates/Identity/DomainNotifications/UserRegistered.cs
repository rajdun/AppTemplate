using Domain.Common.Interfaces;

namespace Domain.Aggregates.Identity.DomainNotifications;

public record UserRegistered(Guid Id, string Name, string Email, string Language) : IDomainNotification;
