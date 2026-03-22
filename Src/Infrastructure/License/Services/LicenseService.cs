using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Common;
using Application.Common.Interfaces;
using Application.Licence.Services;
using Domain.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Licence.Services;

public class LicenceService(IConfiguration configuration, ICacheService cache, IApplicationDbContext dbContext) : ILicenceService
{
    public async Task<LicenceData> DecodeTokenAsync(string token)
    {
        var settings = configuration
            .GetSection(LicenceSettings.SectionName)
            .Get<LicenceSettings>() ?? new LicenceSettings();

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

        return new LicenceData(tenantId, companyName, maxUsers, expiresAt.ToUniversalTime(), activeFeatures);
    }

    public async Task<bool> IsValidAsync()
    {
        var key = CacheKeys.GetLicenceCacheKey;

        var LicenceData = await cache.GetAsync<LicenceData>(key).ConfigureAwait(false);
        if (LicenceData == null)
        {
            LicenceData = await GetLicenceDataFromDbAsync(CancellationToken.None).ConfigureAwait(false);
            if (LicenceData == null)
            {
                return false;
            }
        }

        if (LicenceData.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    private async Task<LicenceData?> GetLicenceDataFromDbAsync(CancellationToken cancellationToken)
    {
        var Licence = await dbContext.Licences.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (Licence == null)
        {
            return null;
        }

        return new LicenceData(Licence.TenantId, Licence.CompanyName, Licence.MaxUsers, Licence.ExpiresAt.DateTime, Licence.ActiveFeatures);
    }
}
