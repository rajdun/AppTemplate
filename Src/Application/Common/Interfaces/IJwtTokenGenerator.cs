using Domain.Entities.Users;

namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    Task<string> GenerateToken(ApplicationUser user);
    string GenerateRefreshToken();
}