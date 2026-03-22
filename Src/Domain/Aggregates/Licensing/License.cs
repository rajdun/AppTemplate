using Domain.Aggregates.Licensing.DomainNotification;
using Domain.Common.Models;

namespace Domain.Aggregates.Licensing;

public class License : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = null!;
    public string RawJwtToken { get; private set; } = null!;
    public DateTimeOffset LastSyncedAt { get; private set; }

    public string CompanyName { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public int MaxUsers { get; private set; }

    private readonly List<string> _activeFeatures = new();
    public IReadOnlyCollection<string> ActiveFeatures => _activeFeatures.AsReadOnly();

    private License()
    {
        Id = Guid.CreateVersion7();
        RawJwtToken = string.Empty;
    }

    private License(string tenantId, string rawJwtToken, string companyName, DateTime expiresAt, int maxUsers,
        IEnumerable<string> features)
    {
        TenantId = tenantId;
        ApplyNewToken(rawJwtToken, companyName, expiresAt, maxUsers, features);
    }

    public static License Create(string tenantId, string rawJwtToken, string companyName, DateTime expiresAt, int maxUsers,
        IEnumerable<string> features)
    {
        return new License(tenantId, rawJwtToken, companyName, expiresAt, maxUsers, features);
    }

    public void Renew(string newRawJwt, string companyName, DateTime newExpiresAt, int newMaxUsers,
        IEnumerable<string> newFeatures)
    {
        ApplyNewToken(newRawJwt, companyName, newExpiresAt, newMaxUsers, newFeatures);
    }

    public void MarkAsSynced()
    {
        LastSyncedAt = DateTime.UtcNow;
    }

    private void ApplyNewToken(string token, string companyName, DateTime expiresAt, int maxUsers,
        IEnumerable<string> features)
    {
        RawJwtToken = token;
        CompanyName = companyName;
        ExpiresAt = expiresAt;
        MaxUsers = maxUsers;
        LastSyncedAt = DateTime.UtcNow;

        _activeFeatures.Clear();
        _activeFeatures.AddRange(features);

        AddDomainNotification(new LicenseRegenerated(TenantId, RawJwtToken));
    }

    public bool IsValid()
    {
        return DateTime.UtcNow <= ExpiresAt;
    }

    public bool IsInGracePeriod(int gracePeriodDays = 14)
    {
        if (IsValid())
        {
            return false;
        }

        var daysExpired = (DateTime.UtcNow - ExpiresAt).TotalDays;
        return daysExpired <= gracePeriodDays;
    }

    public bool HasFeature(string featureName)
    {
        return _activeFeatures.Contains(featureName);
    }
}
