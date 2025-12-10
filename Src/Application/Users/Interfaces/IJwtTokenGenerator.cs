namespace Application.Users.Interfaces;

public interface IJwtTokenGenerator
{
    public string GenerateToken(Guid userId, string firstName, string lastName, string? ipAddress = null);
    public string GenerateRefreshToken();
}
