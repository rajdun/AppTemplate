using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Implementation;

internal class CurrentUser : IUser
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string Email { get; }
    public bool IsAuthenticated { get; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            IsAuthenticated = true;
            UserName = user.Identity.Name ?? string.Empty;
            Email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                UserId = userId;
            }
            else
            {
                UserId = Guid.Empty;
            }
        }
        else
        {
            IsAuthenticated = false;
            UserId = Guid.Empty;
            UserName = string.Empty;
            Email = string.Empty;
        }
    }
}