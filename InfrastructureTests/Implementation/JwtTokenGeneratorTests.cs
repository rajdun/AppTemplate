using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Application.Common.Interfaces;
using Domain.Entities.Users;
using Infrastructure.Implementation;
using Infrastructure.Implementation.Dto;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace InfrastructureTests.Implementation;

public class JwtTokenGeneratorTests
{
    private readonly IJwtTokenGenerator _sut; 
    private readonly JwtSettings _jwtSettings;
    private readonly DateTime _now;
    private readonly ICacheService _cacheService;

    public JwtTokenGeneratorTests()
    {
        _now = DateTime.UtcNow;
        
        // 1. Arrange (Common Setup)
        _jwtSettings = new JwtSettings
        {
            // Use a key long enough for HmacSha256
            Secret = "TestSecretKey12345678901234567890", 
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 15
        };

        // Mock the IOptions<T> wrapper
        var mockOptions = Substitute.For<IOptions<JwtSettings>>();
        mockOptions.Value.Returns(_jwtSettings);
        var datetimeProvider = Substitute.For<IDateTimeProvider>();
        datetimeProvider.UtcNow.Returns(_now);
        _cacheService = Substitute.For<ICacheService>();

        // Create the instance of the class we are testing
        _sut = new JwtTokenGenerator(mockOptions, datetimeProvider, _cacheService);
    }

    [Fact]
    public async Task GenerateToken_ShouldContainCorrectUserClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act
        var tokenString = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var decodedToken = handler.ReadJwtToken(tokenString);

        Assert.Equal(user.Id.ToString(), decodedToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, decodedToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.UserName, decodedToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Single(decodedToken.Claims, x => x.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task GenerateToken_ShouldHaveCorrectIssuerAudienceAndExpiry()
    {
        // Arrange
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "Test@test.test"
        };
        var expectedExpiry = _now.AddMinutes(_jwtSettings.ExpiryMinutes);

        // Act
        var tokenString = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var decodedToken = handler.ReadJwtToken(tokenString);

        Assert.Equal(_jwtSettings.Issuer, decodedToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, decodedToken.Audiences.Single());
        Assert.True(TimeSpan.FromSeconds(1) > expectedExpiry-decodedToken.ValidTo);
    }

    [Fact]
    public async Task GenerateToken_ShouldBeValidAndSignedWithCorrectKey()
    {
        // Arrange
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "Test@test.test"
        };
        var tokenString = await _sut.GenerateToken(user);
        
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No clock skew for tests
        };

        // Act
        var handler = new JwtSecurityTokenHandler();
        var tokenResult = await handler.ValidateTokenAsync(tokenString, validationParameters);
        

        // Assert
        Assert.NotNull(tokenResult);
        Assert.True(tokenResult.IsValid);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyBase64String()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        
        // Check if it's a valid Base64 string
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Convert.FromBase64String(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn64ByteRandomString()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();
        var decodedBytes = Convert.FromBase64String(refreshToken);

        // Assert
        // The original random number was 64 bytes
        Assert.Equal(64, decodedBytes.Length);
    }
    
    [Fact]
    public void GenerateRefreshToken_ShouldReturnDifferentTokensOnEachCall()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }
    
    [Fact]
    public async Task GenerateToken_ShouldSaveJtiInCache()
    {
        // Arrange
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "Test@test.test"
        };
        
        // Act
        await _sut.GenerateToken(user);
    
        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<TimeSpan>());
    }
}

