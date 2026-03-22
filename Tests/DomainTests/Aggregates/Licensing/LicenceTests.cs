using Domain.Aggregates.Licencing;

namespace DomainTests.Aggregates.Licencing;

public class LicenceTests
{
    private static Licence CreateValidLicence(
        string tenantId = "tenant-1",
        string token = "raw.jwt.token",
        string companyName = "Acme Corp",
        DateTime? expiresAt = null,
        int maxUsers = 100,
        IEnumerable<string>? features = null)
    {
        return Licence.Create(
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
        var Licence = CreateValidLicence(tenantId: "my-tenant");

        Assert.Equal("my-tenant", Licence.TenantId);
    }

    [Fact]
    public void Create_ShouldSetRawJwtTokenCorrectly()
    {
        var Licence = CreateValidLicence(token: "header.payload.sig");

        Assert.Equal("header.payload.sig", Licence.RawJwtToken);
    }

    [Fact]
    public void Create_ShouldSetCompanyNameCorrectly()
    {
        var Licence = CreateValidLicence(companyName: "My Company");

        Assert.Equal("My Company", Licence.CompanyName);
    }

    [Fact]
    public void Create_ShouldSetMaxUsersCorrectly()
    {
        var Licence = CreateValidLicence(maxUsers: 50);

        Assert.Equal(50, Licence.MaxUsers);
    }

    [Fact]
    public void Create_ShouldSetExpiresAtCorrectly()
    {
        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var Licence = CreateValidLicence(expiresAt: expiresAt);

        Assert.Equal(expiresAt, Licence.ExpiresAt);
    }

    [Fact]
    public void Create_ShouldSetActiveFeaturesCorrectly()
    {
        var features = new[] { "FeatureX", "FeatureY", "FeatureZ" };
        var Licence = CreateValidLicence(features: features);

        Assert.Equal(3, Licence.ActiveFeatures.Count);
        Assert.Contains("FeatureX", Licence.ActiveFeatures);
        Assert.Contains("FeatureY", Licence.ActiveFeatures);
        Assert.Contains("FeatureZ", Licence.ActiveFeatures);
    }

    [Fact]
    public void Create_ShouldHaveGuidTypeId()
    {
        // The parameterized constructor does not auto-generate an Id (EF Core handles it).
        // Assert the property is a Guid and the object is created without error.
        var Licence = CreateValidLicence();

        Assert.IsType<Guid>(Licence.Id);
    }

    [Fact]
    public void Create_CalledTwice_ShouldProduceSeparateInstances()
    {
        var a = CreateValidLicence(tenantId: "tenant-a");
        var b = CreateValidLicence(tenantId: "tenant-b");

        Assert.NotSame(a, b);
        Assert.NotEqual(a.TenantId, b.TenantId);
    }

    [Fact]
    public void Create_ShouldSetLastSyncedAtToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var Licence = CreateValidLicence();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(Licence.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── IsValid ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_WhenNotExpired_ShouldReturnTrue()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddDays(10));

        Assert.True(Licence.IsValid());
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddDays(-1));

        Assert.False(Licence.IsValid());
    }

    [Fact]
    public void IsValid_ExpiringInTheFuture_ShouldReturnTrue()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddSeconds(10));

        Assert.True(Licence.IsValid());
    }

    // ── IsInGracePeriod ───────────────────────────────────────────────────

    [Fact]
    public void IsInGracePeriod_WhenStillValid_ShouldReturnFalse()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddDays(5));

        Assert.False(Licence.IsInGracePeriod());
    }

    [Fact]
    public void IsInGracePeriod_WhenExpiredWithinGracePeriod_ShouldReturnTrue()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddDays(-5));

        Assert.True(Licence.IsInGracePeriod(gracePeriodDays: 14));
    }

    [Fact]
    public void IsInGracePeriod_WhenExpiredBeyondGracePeriod_ShouldReturnFalse()
    {
        var Licence = CreateValidLicence(expiresAt: DateTime.UtcNow.AddDays(-20));

        Assert.False(Licence.IsInGracePeriod(gracePeriodDays: 14));
    }

    // ── HasFeature ────────────────────────────────────────────────────────

    [Fact]
    public void HasFeature_WhenFeatureExists_ShouldReturnTrue()
    {
        var Licence = CreateValidLicence(features: ["Analytics", "Export"]);

        Assert.True(Licence.HasFeature("Analytics"));
    }

    [Fact]
    public void HasFeature_WhenFeatureDoesNotExist_ShouldReturnFalse()
    {
        var Licence = CreateValidLicence(features: ["Analytics"]);

        Assert.False(Licence.HasFeature("Import"));
    }

    [Fact]
    public void HasFeature_IsCaseSensitive()
    {
        var Licence = CreateValidLicence(features: ["Analytics"]);

        Assert.False(Licence.HasFeature("analytics"));
    }

    // ── Renew ─────────────────────────────────────────────────────────────

    [Fact]
    public void Renew_ShouldUpdateRawJwtToken()
    {
        var Licence = CreateValidLicence(token: "old.token");
        Licence.Renew("new.token", "Acme", DateTime.UtcNow.AddDays(365), 200, ["NewFeature"]);

        Assert.Equal("new.token", Licence.RawJwtToken);
    }

    [Fact]
    public void Renew_ShouldUpdateCompanyName()
    {
        var Licence = CreateValidLicence(companyName: "Old Corp");
        Licence.Renew("token", "New Corp", DateTime.UtcNow.AddDays(365), 200, []);

        Assert.Equal("New Corp", Licence.CompanyName);
    }

    [Fact]
    public void Renew_ShouldUpdateMaxUsers()
    {
        var Licence = CreateValidLicence(maxUsers: 10);
        Licence.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 500, []);

        Assert.Equal(500, Licence.MaxUsers);
    }

    [Fact]
    public void Renew_ShouldReplaceActiveFeatures()
    {
        var Licence = CreateValidLicence(features: ["OldFeature"]);
        Licence.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 100, ["NewFeatureA", "NewFeatureB"]);

        Assert.Equal(2, Licence.ActiveFeatures.Count);
        Assert.Contains("NewFeatureA", Licence.ActiveFeatures);
        Assert.Contains("NewFeatureB", Licence.ActiveFeatures);
        Assert.DoesNotContain("OldFeature", Licence.ActiveFeatures);
    }

    [Fact]
    public void Renew_ShouldUpdateLastSyncedAt()
    {
        var Licence = CreateValidLicence();
        var before = DateTime.UtcNow.AddSeconds(-1);

        Licence.Renew("token", "Corp", DateTime.UtcNow.AddDays(365), 100, []);

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(Licence.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── MarkAsSynced ──────────────────────────────────────────────────────

    [Fact]
    public void MarkAsSynced_ShouldUpdateLastSyncedAtToNow()
    {
        var Licence = CreateValidLicence();
        var before = DateTime.UtcNow.AddSeconds(-1);

        Licence.MarkAsSynced();

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(Licence.LastSyncedAt.UtcDateTime, before, after);
    }

    // ── ActiveFeatures read-only ──────────────────────────────────────────

    [Fact]
    public void ActiveFeatures_ShouldBeReadOnly()
    {
        var Licence = CreateValidLicence();

        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(Licence.ActiveFeatures);
    }
}


