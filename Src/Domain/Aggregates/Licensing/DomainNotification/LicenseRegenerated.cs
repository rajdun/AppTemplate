using Domain.Common.Interfaces;

namespace Domain.Aggregates.Licensing.DomainNotification;

public record LicenseRegenerated(string TenantId, string RawJwtToken) : IDomainNotification;
