using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.ExtensionMethods;
using Application.Common.Interfaces;
using Application.Common.Mailing;
using Application.Common.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Infrastructure.Implementation;

internal class CurrentUser : IUser
{
    // Private constructor ensures instantiation is controlled by the factory.
    private CurrentUser()
    {
    }

    public Guid UserId { get; private init; }
    public string UserName { get; private init; } = string.Empty;
    public string Email { get; private init; } = string.Empty;
    public bool IsAuthenticated { get; private init; }
    public bool IsAdmin { get; private init; }
    public AppLanguage Language { get; private init; }

    /// <summary>
    /// Asynchronously creates and initializes a CurrentUser instance.
    /// </summary>
    public static async Task<IUser> CreateAsync(IHttpContextAccessor httpContextAccessor, ICacheService cacheService)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        var cultureFeature = httpContext?.Features.Get<IRequestCultureFeature>();
        var languageCode = cultureFeature?.RequestCulture.UICulture.TwoLetterISOLanguageName ?? "pl";
        var language = AppLanguageHelpers.FromString(languageCode);

        // Start with an unauthenticated user state.
        var currentUser = new CurrentUser { Language = language };

        if (user?.Identity?.IsAuthenticated != true)
        {
            return currentUser; // Not authenticated
        }

        var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(jti))
        {
            return currentUser; // Token is missing a JTI claim
        }

        // Asynchronously check if the token has been revoked.
        var revokedJti = await cacheService.GetAsync<string>(CacheKeys.GetJtiCacheKey(jti)).ConfigureAwait(false);
        if (revokedJti != "valid")
        {
            return currentUser;
        }

        // If all checks pass, populate the user properties.
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return currentUser;
        }

        return new CurrentUser
        {
            Language = language,
            IsAuthenticated = true,
            UserId = userId,
            UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            IsAdmin = user.IsInRole("Admin") // Check for Admin role claim
        };
    }
}
