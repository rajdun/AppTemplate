using Domain.Aggregates.Identity;
using Domain.Aggregates.Identity.DomainNotifications;

namespace DomainTests.Aggregates.Identity;

public class UserProfileTests
{
    [Fact]
    public void Create_ShouldCreateUserProfileWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var firstName = "Jan";
        var lastName = "Kowalski";
        var email = "jan.kowalski@example.com";
        var language = "pl";

        // Act
        var profile = UserProfile.Create(id, firstName, lastName, email, language);

        // Assert
        Assert.Equal(id, profile.Id);
        Assert.Equal(firstName, profile.FirstName);
        Assert.Equal(lastName, profile.LastName);
        Assert.Equal(email, profile.Email);
        Assert.Null(profile.ArchivedAt);
    }

    [Fact]
    public void Create_ShouldAddUserRegisteredDomainNotification()
    {
        // Arrange
        var id = Guid.NewGuid();
        var firstName = "Jan";
        var lastName = "Kowalski";
        var email = "jan.kowalski@example.com";

        // Act
        var profile = UserProfile.Create(id, firstName, lastName, email);

        // Assert
        Assert.Single(profile.DomainNotifications);
        var notification = profile.DomainNotifications.First() as UserRegistered;
        Assert.NotNull(notification);
        Assert.Equal(id, notification.Id);
        Assert.Equal($"{firstName} {lastName}", notification.Name);
        Assert.Equal(email, notification.Email);
    }

    [Fact]
    public void CanLogin_WhenNotArchived_ShouldReturnTrue()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");

        // Act
        var result = profile.CanLogin();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanLogin_WhenArchived_ShouldReturnFalse()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        profile.Deactivate(DateTimeOffset.UtcNow);

        // Act
        var result = profile.CanLogin();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Deactivate_WhenNotArchived_ShouldSetArchivedAtAndAddNotification()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        var archivedAt = DateTimeOffset.UtcNow;
        profile.ClearDomainNotifications(); // Clear initial notification

        // Act
        profile.Deactivate(archivedAt);

        // Assert
        Assert.NotNull(profile.ArchivedAt);
        Assert.Equal(archivedAt, profile.ArchivedAt);
        Assert.Single(profile.DomainNotifications);
        Assert.IsType<UserDeactivated>(profile.DomainNotifications.First());
    }

    [Fact]
    public void Deactivate_WhenAlreadyArchived_ShouldNotAddNotificationAgain()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        profile.Deactivate(DateTimeOffset.UtcNow);
        profile.ClearDomainNotifications(); // Clear notifications

        // Act
        profile.Deactivate(DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        Assert.Empty(profile.DomainNotifications);
    }

    [Fact]
    public void Update_ShouldUpdateFirstNameAndLastName()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        var newFirstName = "Piotr";
        var newLastName = "Nowak";

        // Act
        profile.Update(newFirstName, newLastName);

        // Assert
        Assert.Equal(newFirstName, profile.FirstName);
        Assert.Equal(newLastName, profile.LastName);
    }
}

