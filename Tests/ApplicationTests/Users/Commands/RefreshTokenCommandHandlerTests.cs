using Application.Common.Interfaces;
using Application.Resources;
using Application.Users.Commands;
using Domain.Common;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly ICacheService _cacheService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUser _currentUser;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _cacheService = Substitute.For<ICacheService>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        _currentUser = Substitute.For<IUser>();
        _handler = new RefreshTokenCommandHandler(
            _cacheService, _jwtTokenGenerator, _userManager, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenNotInCache_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand("refresh-token");
        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            Arg.Any<CancellationToken>()).Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenDoesNotMatch_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand("wrong-token");
        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            Arg.Any<CancellationToken>()).Returns("correct-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid-token";
        var command = new RefreshTokenCommand(token);

        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            Arg.Any<CancellationToken>()).Returns(token);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenUserDeactivated_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid-token";
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        user.DeactivatedAt = DateTimeOffset.UtcNow;
        var command = new RefreshTokenCommand(token);

        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            Arg.Any<CancellationToken>()).Returns(token);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.UserNotActive);
    }

    [Fact]
    public async Task Handle_WhenTokenValid_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldToken = "old-refresh-token";
        var newJwtToken = "new-jwt-token";
        var newRefreshToken = "new-refresh-token";
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var command = new RefreshTokenCommand(oldToken);

        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            Arg.Any<CancellationToken>()).Returns(oldToken);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _jwtTokenGenerator.GenerateToken(user).Returns(Task.FromResult(newJwtToken));
        _jwtTokenGenerator.GenerateRefreshToken().Returns(newRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newJwtToken, result.Value.Token);
        Assert.Equal(newRefreshToken, result.Value.RefreshToken);

        await _cacheService.Received(1).SetAsync(
            CacheKeys.GetRefreshTokenCacheKey(userId.ToString()),
            newRefreshToken,
            TimeSpan.FromDays(7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_WithEmptyRefreshToken_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.RefreshToken));
    }

    [Fact]
    public void Validator_WithValidRefreshToken_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("valid-token");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}

