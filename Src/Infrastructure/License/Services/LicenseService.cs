using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Common;
using Application.Common.Interfaces;
using Application.License.Services;
using Domain.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.License.Services;

public class LicenseService(IConfiguration configuration, ICacheService cache, IApplicationDbContext dbContext) : ILicenseService
{
    public async Task<LicenseData> DecodeTokenAsync(string token)
    {
        var settings = configuration
            .GetSection(LicenseSettings.SectionName)
            .Get<LicenseSettings>() ?? new LicenseSettings();

        var pemContent = await File.ReadAllTextAsync(settings.PublicKeyPath).ConfigureAwait(false);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(pemContent);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false))
        };

        var handler = new JwtSecurityTokenHandler();
        var result = await handler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);

        if (!result.IsValid)
        {
            throw new SecurityTokenException("Invalid token");
        }

        var claims = result.ClaimsIdentity;

        var tenantId = claims.FindFirst("tenantId")?.Value ?? string.Empty;
        var companyName = claims.FindFirst("companyName")?.Value ?? string.Empty;
        var maxUsers = int.TryParse(claims.FindFirst("maxUsers")?.Value, out var mu) ? mu : 0;
        var expiresAt = DateTime.TryParse(claims.FindFirst("expiresAt")?.Value, System.Globalization.CultureInfo.InvariantCulture, out var exp) ? exp : DateTime.MinValue;
        var activeFeatures = claims.Claims
            .Where(c => c.Type == "activeFeatures")
            .Select(c => c.Value);

        return new LicenseData(tenantId, companyName, maxUsers, expiresAt.ToUniversalTime(), activeFeatures);
    }

    public async Task<bool> IsValidAsync()
    {
        var key = CacheKeys.GetLicenseCacheKey;

        var licenseData = await cache.GetAsync<LicenseData>(key).ConfigureAwait(false);
        if (licenseData == null)
        {
            licenseData = await GetLicenseDataFromDbAsync(CancellationToken.None).ConfigureAwait(false);
            if (licenseData == null)
            {
                return false;
            }
        }

        if (licenseData.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    private async Task<LicenseData?> GetLicenseDataFromDbAsync(CancellationToken cancellationToken)
    {
        var license = await dbContext.Licenses.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (license == null)
        {
            return null;
        }

        return new LicenseData(license.TenantId, license.CompanyName, license.MaxUsers, license.ExpiresAt.DateTime, license.ActiveFeatures);
    }
}
