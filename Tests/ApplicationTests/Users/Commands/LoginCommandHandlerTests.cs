using Application.Common.Interfaces;
using Application.Resources;
using Application.Users.Commands;
using Domain.Common;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class LoginCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICacheService _cacheService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _cacheService = Substitute.For<ICacheService>();
        _handler = new LoginCommandHandler(_userManager, _jwtTokenGenerator, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("nonexistent", "password");
        _userManager.FindByNameAsync(command.Username).Returns((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.InvalidPasswordOrUsername);
    }

    [Fact]
    public async Task Handle_WhenPasswordIncorrect_ShouldReturnFailure()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var command = new LoginCommand("testuser", "wrongpassword");

        _userManager.FindByNameAsync(command.Username).Returns(user);
        _userManager.CheckPasswordAsync(user, command.Password).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.InvalidPasswordOrUsername);
    }

    [Fact]
    public async Task Handle_WhenUserDeactivated_ShouldReturnFailure()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        user.DeactivatedAt = DateTimeOffset.UtcNow;
        var command = new LoginCommand("testuser", "password");

        _userManager.FindByNameAsync(command.Username).Returns(user);
        _userManager.CheckPasswordAsync(user, command.Password).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.UserNotActive);
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ShouldReturnTokens()
    {
        // Arrange
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var command = new LoginCommand("testuser", "password");
        var expectedToken = "jwt-token-123";
        var expectedRefreshToken = "refresh-token-456";

        _userManager.FindByNameAsync(command.Username).Returns(user);
        _userManager.CheckPasswordAsync(user, command.Password).Returns(true);
        _jwtTokenGenerator.GenerateToken(user).Returns(Task.FromResult(expectedToken));
        _jwtTokenGenerator.GenerateRefreshToken().Returns(expectedRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedToken, result.Value.Token);
        Assert.Equal(expectedRefreshToken, result.Value.RefreshToken);

        await _cacheService.Received(1).SetAsync(
            CacheKeys.GetRefreshTokenCacheKey(user.Id.ToString()),
            expectedRefreshToken,
            TimeSpan.FromDays(7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_WithEmptyUsername_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("", "password");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Username));
    }

    [Fact]
    public void Validator_WithEmptyPassword_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("username", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Password));
    }

    [Fact]
    public void Validator_WithValidCredentials_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("username", "password");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}

