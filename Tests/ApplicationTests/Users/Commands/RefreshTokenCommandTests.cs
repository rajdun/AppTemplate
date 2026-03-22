using Application.Common.Interfaces;
using Application.Users.Commands;
using Application.Users.Dto;
using Application.Users.Interfaces;
using Domain.Aggregates.Identity;
using Domain.Common.Interfaces;
using FluentResults;
using NSubstitute;

namespace ApplicationTests.Users.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUser _currentUser;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICacheService _cacheService;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandlerTests()
    {
        _currentUser = Substitute.For<IUser>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _cacheService = Substitute.For<ICacheService>();
        _identityService = Substitute.For<IIdentityService>();
    }

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_currentUser, _jwtTokenGenerator, _cacheService, _identityService);

    [Fact]
    public async Task Handle_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>($"user_refresh_token_old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(userId.ToString());
        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _jwtTokenGenerator.GenerateToken(userId, "Jan", "Kowalski").Returns("new.access.token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("new.refresh.token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RefreshTokenCommand("old-refresh-token"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new.access.token", result.Value.Token);
        Assert.Equal("new.refresh.token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_WhenCachedTokenNotFound_ShouldReturnFail()
    {
        // Arrange
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RefreshTokenCommand("unknown-refresh-token"));

        // Assert
        Assert.True(result.IsFailed);
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenCachedUserIdDoesNotMatchCurrentUser_ShouldReturnFail()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        _currentUser.UserId.Returns(currentUserId);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(differentUserId.ToString());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RefreshTokenCommand("some-refresh-token"));

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldSaveNewRefreshTokenToCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Jan", "Kowalski", "jan@example.com");
        profile.ClearDomainEvents();

        _currentUser.UserId.Returns(userId);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId.ToString());
        _identityService.GetUserProfileAsync(userId).Returns(Result.Ok(profile));
        _jwtTokenGenerator.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("access");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("new-refresh");

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RefreshTokenCommand("old-refresh"));

        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("new-refresh")),
            Arg.Any<string>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidRefreshToken_ShouldPass()
    {
        var result = _validator.Validate(new RefreshTokenCommand("valid-refresh-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyRefreshToken_ShouldFail()
    {
        var result = _validator.Validate(new RefreshTokenCommand(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}

