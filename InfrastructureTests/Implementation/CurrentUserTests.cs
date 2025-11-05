using System.Security.Claims;
using Application.Common.ExtensionMethods;
using Application.Common.ValueObjects;
using Infrastructure.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using NSubstitute;

namespace InfrastructureTests.Implementation;

public class CurrentUserTests
{
    [Fact]
    public void CurrentUser_WhenUserIsAuthenticated_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "testuser";
        var email = "test@example.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("email", email)
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

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(userName, currentUser.UserName);
        Assert.Equal(email, currentUser.Email);
        Assert.True(currentUser.IsAdmin);
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public void CurrentUser_WhenUserIsNotAuthenticated_ShouldSetDefaultValues()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
        Assert.Equal(string.Empty, currentUser.UserName);
        Assert.Equal(string.Empty, currentUser.Email);
        Assert.False(currentUser.IsAdmin);
    }

    [Fact]
    public void CurrentUser_WhenHttpContextIsNull_ShouldSetDefaultValues()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
        Assert.Equal(string.Empty, currentUser.UserName);
        Assert.Equal(string.Empty, currentUser.Email);
    }

    [Fact]
    public void CurrentUser_WithEnglishCulture_ShouldSetEnglishLanguage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var requestCultureFeature = new RequestCultureFeature(
            new RequestCulture("en-US"), null);
        httpContext.Features.Set<IRequestCultureFeature>(requestCultureFeature);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.Equal(AppLanguage.En, currentUser.Language);
    }

    [Fact]
    public void CurrentUser_WithPolishCulture_ShouldSetPolishLanguage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var requestCultureFeature = new RequestCultureFeature(
            new RequestCulture("pl-PL"), null);
        httpContext.Features.Set<IRequestCultureFeature>(requestCultureFeature);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public void CurrentUser_WithNoCultureFeature_ShouldDefaultToPolish()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.Equal(AppLanguage.Pl, currentUser.Language);
    }

    [Fact]
    public void CurrentUser_WithInvalidUserIdClaim_ShouldSetEmptyGuid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid"),
            new("email", "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var currentUser = new CurrentUser(httpContextAccessor);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
    }
}

