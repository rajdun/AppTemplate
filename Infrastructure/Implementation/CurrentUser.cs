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
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var cultureFeature = httpContextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>();
        var languageCode = cultureFeature?.RequestCulture.UICulture.TwoLetterISOLanguageName ?? "pl";

        Language = AppLanguageHelpers.FromString(languageCode);
        
        if (user?.Identity?.IsAuthenticated ?? false)
        {
            IsAuthenticated = true;
            UserName = user.Identity.Name ?? string.Empty;
            Email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            IsAdmin = true;

            if (Guid.TryParse(userIdClaim, out var userId))
                UserId = userId;
            else
                UserId = Guid.Empty;
        }
        else
        {
            IsAuthenticated = false;
            UserId = Guid.Empty;
            UserName = string.Empty;
            Email = string.Empty;
        }
    }

    public Guid UserId { get; }
    public string UserName { get; }
    public string Email { get; }
    public bool IsAuthenticated { get; }
    public bool IsAdmin { get; }
    public AppLanguage Language { get; }
}