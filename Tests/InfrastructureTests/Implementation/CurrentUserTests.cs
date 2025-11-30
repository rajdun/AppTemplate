using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.ExtensionMethods;
using Application.Common.Interfaces;
using Application.Common.ValueObjects;
using Infrastructure.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using NSubstitute;

namespace InfrastructureTests.Implementation;

public class CurrentUserTests
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();

    [Fact]
    public async Task CurrentUser_WhenUserIsAuthenticated_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "testuser";
        var email = "test@example.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("email", email),
            new(JwtRegisteredClaimNames.Jti, "some-jti-value"),
            new(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        httpContext.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }));

        var requestCultureFeature = new RequestCultureFeature(
            new RequestCulture("pl-PL"), null);
        httpContext.Features.Set<IRequestCultureFeature>(requestCultureFeature);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(userName, currentUser.UserName);
        Assert.Equal(email, currentUser.Email);
        Assert.False(currentUser.IsAdmin);
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public async Task CurrentUser_WhenUserIsNotAuthenticated_ShouldSetDefaultValues()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
        Assert.Equal(string.Empty, currentUser.UserName);
        Assert.Equal(string.Empty, currentUser.Email);
        Assert.False(currentUser.IsAdmin);
    }

    [Fact]
    public async Task CurrentUser_WhenHttpContextIsNull_ShouldSetDefaultValues()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
        Assert.Equal(string.Empty, currentUser.UserName);
        Assert.Equal(string.Empty, currentUser.Email);
    }

    [Fact]
    public async Task CurrentUser_WithEnglishCulture_ShouldSetEnglishLanguage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var requestCultureFeature = new RequestCultureFeature(
            new RequestCulture("en-US"), null);
        httpContext.Features.Set<IRequestCultureFeature>(requestCultureFeature);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.Equal(AppLanguage.En, currentUser.Language);
    }

    [Fact]
    public async Task CurrentUser_WithPolishCulture_ShouldSetPolishLanguage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var requestCultureFeature = new RequestCultureFeature(
            new RequestCulture("pl-PL"), null);
        httpContext.Features.Set<IRequestCultureFeature>(requestCultureFeature);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public async Task CurrentUser_WithNoCultureFeature_ShouldDefaultToPolish()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public async Task CurrentUser_WithInvalidUserIdClaim_ShouldSetEmptyGuid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid"),
            new("email", "test@example.com"),
            new(JwtRegisteredClaimNames.Jti, "some-jti-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        _cacheService.GetAsync<string>(Arg.Any<string>()).Returns(Task.FromResult<string?>("valid"));

        // Act
        var currentUser = await CurrentUser.CreateAsync(httpContextAccessor, _cacheService);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
    }
}

