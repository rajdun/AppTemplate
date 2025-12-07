using Domain.Aggregates.Identity;

namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    public Task<string> GenerateToken(User user);
    public string GenerateRefreshToken();
}
