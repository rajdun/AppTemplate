using Application.Common.Interfaces;
using Application.Common.Mediator;
using NSubstitute;

namespace ApplicationTests.Common.Mediator;

public class AuthorizeAttributeTests
{
    [Fact]
    public void AuthorizeAttribute_ShouldSetPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeAttribute(AuthorizePolicy.Admin);

        // Assert
        Assert.Equal(AuthorizePolicy.Admin, attribute.AuthorizeUserBehaviour);
    }

    [Fact]
    public void Authorize_WithUnauthenticatedUser_ShouldReturnFalse()
    {
        // Arrange
        var attribute = new AuthorizeAttribute(AuthorizePolicy.Admin);
        var user = Substitute.For<IUser>();
        user.IsAuthenticated.Returns(false);

        // Act
        var result = attribute.Authorize(user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_WithNonePolicy_ShouldReturnTrueForAuthenticatedUser()
    {
        // Arrange
        var attribute = new AuthorizeAttribute(AuthorizePolicy.None);
        var user = Substitute.For<IUser>();
        user.IsAuthenticated.Returns(true);

        // Act
        var result = attribute.Authorize(user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Authorize_WithAdminPolicyAndAdminUser_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new AuthorizeAttribute(AuthorizePolicy.Admin);
        var user = Substitute.For<IUser>();
        user.IsAuthenticated.Returns(true);
        user.IsAdmin.Returns(true);

        // Act
        var result = attribute.Authorize(user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Authorize_WithAdminPolicyAndNonAdminUser_ShouldReturnFalse()
    {
        // Arrange
        var attribute = new AuthorizeAttribute(AuthorizePolicy.Admin);
        var user = Substitute.For<IUser>();
        user.IsAuthenticated.Returns(true);
        user.IsAdmin.Returns(false);

        // Act
        var result = attribute.Authorize(user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_WithUserPolicy_ShouldReturnFalseWhenNotAdmin()
    {
        // Arrange
        var attribute = new AuthorizeAttribute(AuthorizePolicy.User);
        var user = Substitute.For<IUser>();
        user.IsAuthenticated.Returns(true);
        user.IsAdmin.Returns(false);

        // Act
        var result = attribute.Authorize(user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizePolicy_ShouldSupportFlags()
    {
        // Arrange & Act
        var combined = AuthorizePolicy.User | AuthorizePolicy.Admin;

        // Assert
        Assert.True(combined.HasFlag(AuthorizePolicy.User));
        Assert.True(combined.HasFlag(AuthorizePolicy.Admin));
    }

    [Fact]
    public void AuthorizePolicy_NoneShouldBeZero()
    {
        // Arrange & Act
        var none = AuthorizePolicy.None;

        // Assert
        Assert.Equal(0, (int)none);
    }
}

