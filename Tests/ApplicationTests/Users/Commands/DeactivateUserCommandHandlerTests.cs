using Application.Resources;
using Application.Users.Commands;
using Application.Users.Dto;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class DeactivateUserCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests()
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        _logger = Substitute.For<ILogger<DeactivateUserCommandHandler>>();
        _handler = new DeactivateUserCommandHandler(_userManager, _logger);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeactivateUserCommand(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyDeactivated_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        user.DeactivatedAt = DateTimeOffset.UtcNow;
        var command = new DeactivateUserCommand(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == UserTranslations.UserNotActive);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var command = new DeactivateUserCommand(userId);
        var identityErrors = new[] { new IdentityError { Description = "Update failed" } };
        
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == "Update failed");
    }

    [Fact]
    public async Task Handle_WhenDeactivationSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ApplicationUser.Create("testuser", "test@test.com", "en");
        var command = new DeactivateUserCommand(userId);
        
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(user.DeactivatedAt);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(user.UserName, result.Value.Name);
        Assert.Equal(user.Email, result.Value.Email);
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public void Validator_WithEmptyUserId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new DeactivateUserCommandValidator();
        var command = new DeactivateUserCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public void Validator_WithValidUserId_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new DeactivateUserCommandValidator();
        var command = new DeactivateUserCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}