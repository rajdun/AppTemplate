using Application.Common.Interfaces;
using Application.Users.Commands;
using Application.Users.Interfaces;
using Domain.Aggregates.Identity;
using FluentResults;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class RegisterCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICacheService _cacheService;

    public RegisterCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _cacheService = Substitute.For<ICacheService>();
    }

    private RegisterCommandHandler CreateHandler() =>
        new(_identityService, _jwtTokenGenerator, _cacheService);

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnTokenResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Ok(userId));
        _jwtTokenGenerator.GenerateToken(userId, "Jan", "Kowalski").Returns("access.token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh.token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterCommand("Jan Kowalski", "jan@example.com", "SecurePass1!", "SecurePass1!"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("access.token", result.Value.Token);
        Assert.Equal("refresh.token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_WithSingleWordUsername_ShouldUseUsernameAsFirstName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityService.CreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(),
                "Jan", "")
            .Returns(Result.Ok(userId));
        _jwtTokenGenerator.GenerateToken(userId, "Jan", "").Returns("token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterCommand("Jan", "jan@example.com", "Password1!", "Password1!"));

        // Assert
        Assert.True(result.IsSuccess);
        await _identityService.Received(1).CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), "Jan", "");
    }

    [Fact]
    public async Task Handle_WhenCreateUserFails_ShouldReturnFail()
    {
        // Arrange
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Fail<Guid>("Email already taken"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterCommand("Jan", "jan@example.com", "Password1!", "Password1!"));

        // Assert
        Assert.True(result.IsFailed);
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldSaveRefreshTokenToCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Ok(userId));
        _jwtTokenGenerator.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("access");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh");

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RegisterCommand("Jan", "jan@example.com", "Password1!", "Password1!"));

        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("refresh")),
            Arg.Any<string>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(
            new RegisterCommand("Jan Kowalski", "jan@example.com", "Password123!", "Password123!"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyUsername_ShouldFail()
    {
        var result = _validator.Validate(
            new RegisterCommand("", "jan@example.com", "Password123!", "Password123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Username));
    }

    [Fact]
    public void Validate_WithTooShortUsername_ShouldFail()
    {
        var result = _validator.Validate(
            new RegisterCommand("J", "jan@example.com", "Password123!", "Password123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Username));
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var result = _validator.Validate(
            new RegisterCommand("Jan", "not-an-email", "Password123!", "Password123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var result = _validator.Validate(
            new RegisterCommand("Jan", "jan@example.com", "short", "short"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldFail()
    {
        var result = _validator.Validate(
            new RegisterCommand("Jan", "jan@example.com", "Password123!", "Different123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.RepeatPassword));
    }
}

