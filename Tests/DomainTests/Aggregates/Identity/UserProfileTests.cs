using Domain.Aggregates.Identity;
using Domain.Aggregates.Identity.DomainEvents;

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
    public void Create_ShouldAddUserRegisteredDomainEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var firstName = "Jan";
        var lastName = "Kowalski";
        var email = "jan.kowalski@example.com";

        // Act
        var profile = UserProfile.Create(id, firstName, lastName, email);

        // Assert
        Assert.Single(profile.DomainEvents);
        var domainEvent = profile.DomainEvents.First() as UserRegistered;
        Assert.NotNull(domainEvent);
        Assert.Equal(id, domainEvent.Id);
        Assert.Equal($"{firstName} {lastName}", domainEvent.Name);
        Assert.Equal(email, domainEvent.Email);
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
    public void Deactivate_WhenNotArchived_ShouldSetArchivedAtAndAddEvent()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        var archivedAt = DateTimeOffset.UtcNow;
        profile.ClearDomainEvents(); // Clear initial event

        // Act
        profile.Deactivate(archivedAt);

        // Assert
        Assert.NotNull(profile.ArchivedAt);
        Assert.Equal(archivedAt, profile.ArchivedAt);
        Assert.Single(profile.DomainEvents);
        Assert.IsType<UserDeactivated>(profile.DomainEvents.First());
    }

    [Fact]
    public void Deactivate_WhenAlreadyArchived_ShouldNotAddEventAgain()
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), "Jan", "Kowalski", "jan@example.com");
        profile.Deactivate(DateTimeOffset.UtcNow);
        profile.ClearDomainEvents(); // Clear events

        // Act
        profile.Deactivate(DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        Assert.Empty(profile.DomainEvents);
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

