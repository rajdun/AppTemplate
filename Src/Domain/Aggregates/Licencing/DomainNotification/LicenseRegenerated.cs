using Domain.Common.Interfaces;

namespace Domain.Aggregates.Licencing.DomainNotification;

public record LicenceRegenerated(string TenantId, string RawJwtToken) : IDomainNotification;
