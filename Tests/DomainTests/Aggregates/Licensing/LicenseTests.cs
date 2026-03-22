using Domain.Aggregates.Licensing;

namespace DomainTests.Aggregates.Licensing;

public class LicenseTests
{
    private static License CreateValidLicense(
        string tenantId = "tenant-1",
        string token = "raw.jwt.token",
        string companyName = "Acme Corp",
        DateTime? expiresAt = null,
        int maxUsers = 100,
        IEnumerable<string>? features = null)
    {
        return License.Create(
            tenantId,
            token,
            companyName,
            expiresAt ?? DateTime.UtcNow.AddDays(30),
            maxUsers,
            features ?? ["FeatureA", "FeatureB"]);
    }

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetTenantIdCorrectly()
    {
        var license = CreateValidLicense(tenantId: "my-tenant");

        Assert.Equal("my-tenant", license.TenantId);
    }

    [Fact]
    public void Create_ShouldSetRawJwtTokenCorrectly()
    {
        var license = CreateValidLicense(token: "header.payload.sig");

        Assert.Equal("header.payload.sig", license.RawJwtToken);
    }

    [Fact]
    public void Create_ShouldSetCompanyNameCorrectly()
    {
        var license = CreateValidLicense(companyName: "My Company");

        Assert.Equal("My Company", license.CompanyName);
    }

    [Fact]
    public void Create_ShouldSetMaxUsersCorrectly()
    {
        var license = CreateValidLicense(maxUsers: 50);

        Assert.Equal(50, license.MaxUsers);
    }

    [Fact]
    public void Create_ShouldSetExpiresAtCorrectly()
    {
        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var license = CreateValidLicense(expiresAt: expiresAt);

        Assert.Equal(expiresAt, license.ExpiresAt);
    }

    [Fact]
    public void Create_ShouldSetActiveFeaturesCorrectly()
    {
        var features = new[] { "FeatureX", "FeatureY", "FeatureZ" };
        var license = CreateValidLicense(features: features);

        Assert.Equal(3, license.ActiveFeatures.Count);
        Assert.Contains("FeatureX", license.ActiveFeatures);
        Assert.Contains("FeatureY", license.ActiveFeatures);
        Assert.Contains("FeatureZ", license.ActiveFeatures);
    }

    [Fact]
    public void Create_ShouldHaveGuidTypeId()
    {
        // The parameterized constructor does not auto-generate an Id (EF Core handles it).
        // Assert the property is a Guid and the object is created without error.
        var license = CreateValidLicense();

        Assert.IsType<Guid>(license.Id);
    }

    [Fact]
    public void Create_CalledTwice_ShouldProduceSeparateInstances()
    {
        var a = CreateValidLicense(tenantId: "tenant-a");
        var b = CreateValidLicense(tenantId: "tenant-b");

        Assert.NotSame(a, b);
        Assert.NotEqual(a.TenantId, b.TenantId);
    }

    [Fact]
    public void Create_ShouldSetLastSyncedAtToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var license = CreateValidLicense();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(license.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── IsValid ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_WhenNotExpired_ShouldReturnTrue()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddDays(10));

        Assert.True(license.IsValid());
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddDays(-1));

        Assert.False(license.IsValid());
    }

    [Fact]
    public void IsValid_ExpiringInTheFuture_ShouldReturnTrue()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddSeconds(10));

        Assert.True(license.IsValid());
    }

    // ── IsInGracePeriod ───────────────────────────────────────────────────

    [Fact]
    public void IsInGracePeriod_WhenStillValid_ShouldReturnFalse()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddDays(5));

        Assert.False(license.IsInGracePeriod());
    }

    [Fact]
    public void IsInGracePeriod_WhenExpiredWithinGracePeriod_ShouldReturnTrue()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddDays(-5));

        Assert.True(license.IsInGracePeriod(gracePeriodDays: 14));
    }

    [Fact]
    public void IsInGracePeriod_WhenExpiredBeyondGracePeriod_ShouldReturnFalse()
    {
        var license = CreateValidLicense(expiresAt: DateTime.UtcNow.AddDays(-20));

        Assert.False(license.IsInGracePeriod(gracePeriodDays: 14));
    }

    // ── HasFeature ────────────────────────────────────────────────────────

    [Fact]
    public void HasFeature_WhenFeatureExists_ShouldReturnTrue()
    {
        var license = CreateValidLicense(features: ["Analytics", "Export"]);

        Assert.True(license.HasFeature("Analytics"));
    }

    [Fact]
    public void HasFeature_WhenFeatureDoesNotExist_ShouldReturnFalse()
    {
        var license = CreateValidLicense(features: ["Analytics"]);

        Assert.False(license.HasFeature("Import"));
    }

    [Fact]
    public void HasFeature_IsCaseSensitive()
    {
        var license = CreateValidLicense(features: ["Analytics"]);

        Assert.False(license.HasFeature("analytics"));
    }

    // ── Renew ─────────────────────────────────────────────────────────────

    [Fact]
    public void Renew_ShouldUpdateRawJwtToken()
    {
        var license = CreateValidLicense(token: "old.token");
        license.Renew("new.token", "Acme", DateTime.UtcNow.AddDays(365), 200, ["NewFeature"]);

        Assert.Equal("new.token", license.RawJwtToken);
    }

    [Fact]
    public void Renew_ShouldUpdateCompanyName()
    {
        var license = CreateValidLicense(companyName: "Old Corp");
        license.Renew("token", "New Corp", DateTime.UtcNow.AddDays(365), 200, []);

        Assert.Equal("New Corp", license.CompanyName);
    }

    [Fact]
    public void Renew_ShouldUpdateMaxUsers()
    {
        var license = CreateValidLicense(maxUsers: 10);
        license.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 500, []);

        Assert.Equal(500, license.MaxUsers);
    }

    [Fact]
    public void Renew_ShouldReplaceActiveFeatures()
    {
        var license = CreateValidLicense(features: ["OldFeature"]);
        license.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 100, ["NewFeatureA", "NewFeatureB"]);

        Assert.Equal(2, license.ActiveFeatures.Count);
        Assert.Contains("NewFeatureA", license.ActiveFeatures);
        Assert.Contains("NewFeatureB", license.ActiveFeatures);
        Assert.DoesNotContain("OldFeature", license.ActiveFeatures);
    }

    [Fact]
    public void Renew_ShouldUpdateLastSyncedAt()
    {
        var license = CreateValidLicense();
        var before = DateTime.UtcNow.AddSeconds(-1);

        license.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 100, []);

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(license.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── MarkAsSynced ──────────────────────────────────────────────────────

    [Fact]
    public void MarkAsSynced_ShouldUpdateLastSyncedAtToNow()
    {
        var license = CreateValidLicense();
        var before = DateTime.UtcNow.AddSeconds(-1);

        license.MarkAsSynced();

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(license.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── ActiveFeatures read-only ──────────────────────────────────────────

    [Fact]
    public void ActiveFeatures_ShouldBeReadOnly()
    {
        var license = CreateValidLicense();

        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(license.ActiveFeatures);
    }
}


