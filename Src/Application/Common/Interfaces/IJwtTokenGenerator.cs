using Domain.Entities.Users;

namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    public Task<string> GenerateToken(ApplicationUser user);
    public string GenerateRefreshToken();
}
