using Application.Common.Interfaces;
using Application.Users.Commands;
using Application.Users.Dto;
using Application.Users.Interfaces;
using Domain.Aggregates.Identity;
using FluentResults;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICacheService _cacheService;

    public LoginCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _cacheService = Substitute.For<ICacheService>();
    }

    private LoginCommandHandler CreateHandler() =>
        new(_identityService, _jwtTokenGenerator, _cacheService);

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokenResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _identityService.ValidateCredentialsAsync("jan@example.com", "password")
            .Returns(Result.Ok(userId));
        _identityService.GetUserProfileAsync(userId)
            .Returns(Result.Ok(profile));
        _jwtTokenGenerator.GenerateToken(userId, "Jan", "Kowalski").Returns("access.token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh.token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("jan@example.com", "password"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("access.token", result.Value.Token);
        Assert.Equal("refresh.token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_WhenCredentialsInvalid_ShouldReturnFail()
    {
        // Arrange
        _identityService.ValidateCredentialsAsync("bad@example.com", "wrong")
            .Returns(Result.Fail<Guid>("Invalid credentials"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("bad@example.com", "wrong"));

        // Assert
        Assert.True(result.IsFailed);
        await _identityService.DidNotReceive().GetUserProfileAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenGetUserProfileFails_ShouldReturnFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Ok(userId));
        _identityService.GetUserProfileAsync(userId)
            .Returns(Result.Fail<UserProfile>("User not found"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("jan@example.com", "password"));

        // Assert
        Assert.True(result.IsFailed);
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldSaveRefreshTokenToCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Ok(userId));
        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _jwtTokenGenerator.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("access");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh");

        var handler = CreateHandler();

        // Act
        await handler.Handle(new LoginCommand("jan@example.com", "password"));

        // Assert — the extension method stores via SetAsync with the expected key pattern
        await _cacheService.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("refresh")),
            Arg.Any<string>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", "password123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var result = _validator.Validate(new LoginCommand(string.Empty, "password"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var result = _validator.Validate(new LoginCommand("not-an-email", "password"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
    }
}

