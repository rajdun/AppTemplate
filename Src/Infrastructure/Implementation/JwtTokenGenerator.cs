using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Domain.Aggregates.Identity;
using Infrastructure.Implementation.Dto;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Implementation;

internal class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings, IDateTimeProvider dateTimeProvider, ICacheService cacheService)
    {
        _jwtSettings = jwtSettings;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
    }

    public async Task<string> GenerateToken(User user)
    {
        var jti = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jti.ToString()),
        };


        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtSettings.Value.Issuer,
            _jwtSettings.Value.Audience,
            claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(_jwtSettings.Value.ExpiryMinutes),
            signingCredentials: credentials);

        await _cacheService.SetAsync(CacheKeys.GetJtiCacheKey(jti.ToString()), "valid", TimeSpan.FromMinutes(_jwtSettings.Value.ExpiryMinutes)).ConfigureAwait(false);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
