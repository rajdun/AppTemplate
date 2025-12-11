using Application.Common.Interfaces;
using Application.Users.ExtensionMethods;
using NSubstitute;

namespace ApplicationTests.Users.ExtensionMethods;

public class UserCacheExtensionMethodsTests
{
    private readonly ICacheService _cacheService;

    public UserCacheExtensionMethodsTests()
    {
        _cacheService = Substitute.For<ICacheService>();
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ShouldSaveTokenWithCorrectKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "test-refresh-token";
        var expiration = TimeSpan.FromDays(7);
        var expectedKey = $"user_refresh_token_{refreshToken}";

        // Act
        await _cacheService.SaveRefreshTokenAsync(userId, refreshToken, expiration);

        // Assert
        await _cacheService.Received(1).SetAsync(
            expectedKey,
            userId.ToString(),
            expiration,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenTokenExists_ShouldReturnUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "test-refresh-token";
        var expectedKey = $"user_refresh_token_{refreshToken}";

        _cacheService.GetAsync<string>(expectedKey, Arg.Any<CancellationToken>())
            .Returns(userId.ToString());

        // Act
        var result = await _cacheService.GetRefreshTokenAsync(refreshToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Value);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenTokenDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var refreshToken = "non-existent-token";
        var expectedKey = $"user_refresh_token_{refreshToken}";

        _cacheService.GetAsync<string>(expectedKey, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        var result = await _cacheService.GetRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenTokenIsInvalidGuid_ShouldReturnNull()
    {
        // Arrange
        var refreshToken = "test-refresh-token";
        var expectedKey = $"user_refresh_token_{refreshToken}";

        _cacheService.GetAsync<string>(expectedKey, Arg.Any<CancellationToken>())
            .Returns("invalid-guid");

        // Act
        var result = await _cacheService.GetRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRefreshTokenAsync_ShouldRemoveTokenWithCorrectKey()
    {
        // Arrange
        var refreshToken = "test-refresh-token";
        var expectedKey = $"user_refresh_token_{refreshToken}";

        // Act
        await _cacheService.RemoveRefreshTokenAsync(refreshToken);

        // Assert
        await _cacheService.Received(1).RemoveAsync(
            expectedKey,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_WithCancellationToken_ShouldPassTokenToCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "test-refresh-token";
        var expiration = TimeSpan.FromDays(7);
        var cancellationToken = new CancellationToken();

        // Act
        await _cacheService.SaveRefreshTokenAsync(userId, refreshToken, expiration, cancellationToken);

        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            expiration,
            cancellationToken);
    }
}

