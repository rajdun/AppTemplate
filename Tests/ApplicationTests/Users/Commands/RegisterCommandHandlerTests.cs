using Application.Common.Interfaces;
using Application.Common.ValueObjects;
using Application.Resources;
using Application.Users.Commands;
using Domain.Common;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class RegisterCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICacheService _cacheService;
    private readonly IUser _user;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _cacheService = Substitute.For<ICacheService>();
        _user = Substitute.For<IUser>();
        _user.Language.Returns(AppLanguage.En);
        _handler = new RegisterCommandHandler(_userManager, _jwtTokenGenerator, _cacheService, _user);
    }

    [Fact]
    public async Task Handle_WhenUsernameExists_ShouldReturnFailure()
    {
        // Arrange
        var existingUser = ApplicationUser.Create("existinguser", "existing@test.com", "en");
        var command = new RegisterCommand("existinguser", "new@test.com", "Password123!", "Password123!");

        _userManager.FindByNameAsync(command.Username).Returns(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.UsernameAlreadyExists);
    }

    [Fact]
    public async Task Handle_WhenCreateUserFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand("newuser", "new@test.com", "Password123!", "Password123!");
        var identityErrors = new[] { new IdentityError { Description = "Password too weak" } };

        _userManager.FindByNameAsync(command.Username).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), command.Password)
            .Returns(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == "Password too weak");
    }

    [Fact]
    public async Task Handle_WhenRegistrationSuccessful_ShouldReturnTokens()
    {
        // Arrange
        var command = new RegisterCommand("newuser", "new@test.com", "Password123!", "Password123!");
        var expectedToken = "jwt-token";
        var expectedRefreshToken = "refresh-token";

        _userManager.FindByNameAsync(command.Username).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), command.Password)
            .Returns(IdentityResult.Success);
        _jwtTokenGenerator.GenerateToken(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(expectedToken));
        _jwtTokenGenerator.GenerateRefreshToken().Returns(expectedRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedToken, result.Value.Token);
        Assert.Equal(expectedRefreshToken, result.Value.RefreshToken);

        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.UserName == command.Username && u.Email == command.Email),
            command.Password);

        await _cacheService.Received(1).SetAsync(
            Arg.Any<string>(),
            expectedRefreshToken,
            TimeSpan.FromDays(7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_WithEmptyUsername_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("", "test@test.com", "Password123!", "Password123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Username));
    }

    [Fact]
    public void Validator_WithInvalidEmail_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("username", "invalid-email", "Password123!", "Password123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Email));
    }

    [Fact]
    public void Validator_WithShortPassword_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("username", "test@test.com", "short", "short");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Password));
    }

    [Fact]
    public void Validator_WithMismatchedPasswords_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("username", "test@test.com", "Password123!", "DifferentPassword!");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.RepeatPassword));
    }

    [Fact]
    public void Validator_WithValidData_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("username", "test@test.com", "Password123!", "Password123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_WithNullEmail_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("username", null, "Password123!", "Password123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}

