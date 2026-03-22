using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Application.Common;
using Infrastructure.License;
using Infrastructure.License.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Application.Common.Interfaces;
using NSubstitute;

namespace InfrastructureTests.License;

public class LicenseServiceTests : IDisposable
{
    private readonly RSA _rsa;
    private readonly string _publicKeyPem;
    private readonly string _tempPublicKeyPath;

    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public LicenseServiceTests()
    {
        _rsa = RSA.Create(2048);
        _publicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();

        _tempPublicKeyPath = Path.GetTempFileName();
        File.WriteAllText(_tempPublicKeyPath, _publicKeyPem);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _rsa.Dispose();
        if (File.Exists(_tempPublicKeyPath))
        {
            File.Delete(_tempPublicKeyPath);
        }
    }

    private IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LicenseSettings.SectionName}:PublicKeyPath"] = _tempPublicKeyPath,
                [$"{LicenseSettings.SectionName}:Issuer"] = TestIssuer,
                [$"{LicenseSettings.SectionName}:Audience"] = TestAudience
            })
            .Build();

    private string MintToken(
        string tenantId = "tenant-abc",
        string companyName = "Acme Corp",
        int maxUsers = 100,
        DateTime? expiresAt = null,
        IEnumerable<string>? features = null,
        bool expired = false)
    {
        var expiry = expired
            ? DateTime.UtcNow.AddMinutes(-5)
            : (expiresAt ?? DateTime.UtcNow.AddDays(30));

        var claims = new List<Claim>
        {
            new("tenantId", tenantId),
            new("companyName", companyName),
            new("maxUsers", maxUsers.ToString()),
            new("expiresAt", expiry.ToString("O"))
        };

        foreach (var f in features ?? ["FeatureA", "FeatureB"])
        {
            claims.Add(new Claim("activeFeatures", f));
        }

        var key = new RsaSecurityKey(_rsa.ExportParameters(true));
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task DecodeTokenAsync_WithValidToken_ShouldReturnCorrectTenantId()
    {
        var token = MintToken(tenantId: "my-tenant");
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        var result = await service.DecodeTokenAsync(token);

        Assert.Equal("my-tenant", result.TenantId);
    }

    [Fact]
    public async Task DecodeTokenAsync_WithValidToken_ShouldReturnCorrectCompanyName()
    {
        var token = MintToken(companyName: "Big Corp");
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        var result = await service.DecodeTokenAsync(token);

        Assert.Equal("Big Corp", result.CompanyName);
    }

    [Fact]
    public async Task DecodeTokenAsync_WithValidToken_ShouldReturnCorrectMaxUsers()
    {
        var token = MintToken(maxUsers: 250);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        var result = await service.DecodeTokenAsync(token);

        Assert.Equal(250, result.MaxUsers);
    }

    [Fact]
    public async Task DecodeTokenAsync_WithValidToken_ShouldReturnActiveFeatures()
    {
        var token = MintToken(features: ["Analytics", "Export", "Import"]);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        var result = await service.DecodeTokenAsync(token);

        Assert.Contains("Analytics", result.ActiveFeatures);
        Assert.Contains("Export", result.ActiveFeatures);
        Assert.Contains("Import", result.ActiveFeatures);
    }

    [Fact]
    public async Task DecodeTokenAsync_WithExpiredToken_ShouldThrowSecurityTokenException()
    {
        var token = MintToken(expired: true);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.DecodeTokenAsync(token));
    }

    [Fact]
    public async Task DecodeTokenAsync_WithTamperedToken_ShouldThrowSecurityTokenException()
    {
        var token = MintToken();
        var tampered = token[..^5] + "XXXXX"; // corrupt the signature
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.DecodeTokenAsync(tampered));
    }

    [Fact]
    public async Task DecodeTokenAsync_WithWrongIssuer_ShouldThrowSecurityTokenException()
    {
        // Mint with a different issuer
        var key = new RsaSecurityKey(_rsa.ExportParameters(true));
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var badToken = new JwtSecurityToken(
            issuer: "WrongIssuer",
            audience: TestAudience,
            claims: [new Claim("tenantId", "t")],
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(badToken);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.DecodeTokenAsync(tokenStr));
    }

    [Fact]
    public async Task DecodeTokenAsync_WithWrongAudience_ShouldThrowSecurityTokenException()
    {
        var key = new RsaSecurityKey(_rsa.ExportParameters(true));
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var badToken = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: "WrongAudience",
            claims: [new Claim("tenantId", "t")],
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(badToken);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.DecodeTokenAsync(tokenStr));
    }

    [Fact]
    public async Task DecodeTokenAsync_WithValidToken_ExpiresAtShouldBeUtc()
    {
        var expectedExpiry = new DateTime(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var token = MintToken(expiresAt: expectedExpiry);
        var service = new LicenseService(BuildConfiguration(), Substitute.For<ICacheService>(), Substitute.For<IApplicationDbContext>());

        var result = await service.DecodeTokenAsync(token);

        Assert.Equal(DateTimeKind.Utc, result.ExpiresAt.Kind);
    }
}

