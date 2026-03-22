using Application.Users.Commands;
using Application.Users.Dto;
using Application.Users.Interfaces;
using Domain.Aggregates.Identity;
using FluentResults;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class DeactivateUserCommandHandlerTests
{
    private readonly IIdentityService _identityService;

    public DeactivateUserCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
    }

    private DeactivateUserCommandHandler CreateHandler() =>
        new(_identityService);

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnDeactivateUserResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _identityService.DeactivateUserAsync(userId).Returns(Result.Ok());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateUserCommand(userId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("Jan Kowalski", result.Value.Name);
        Assert.Equal("jan@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenGetUserProfileFails_ShouldReturnFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityService.GetUserProfileAsync(userId).Returns(Result.Fail<UserProfile>("Not found"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateUserCommand(userId));

        // Assert
        Assert.True(result.IsFailed);
        await _identityService.DidNotReceive().DeactivateUserAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenDeactivateFails_ShouldReturnFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _identityService.DeactivateUserAsync(userId).Returns(Result.Fail("Already deactivated"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateUserCommand(userId));

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WithUserHavingOnlyFirstName_ShouldTrimFullName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "", "jan@example.com");
        profile.ClearDomainEvents();

        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _identityService.DeactivateUserAsync(userId).Returns(Result.Ok());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateUserCommand(userId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Jan", result.Value.Name);
    }
}

public class DeactivateUserCommandValidatorTests
{
    private readonly DeactivateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldPass()
    {
        var result = _validator.Validate(new DeactivateUserCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        var result = _validator.Validate(new DeactivateUserCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeactivateUserCommand.UserId));
    }
}

