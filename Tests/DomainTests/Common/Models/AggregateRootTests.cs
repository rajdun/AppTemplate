using Domain.Common.Interfaces;
using Domain.Common.Models;

namespace DomainTests.Common.Models;

public class AggregateRootTests
{
    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id)
        {
            Id = id;
        }

        public void AddTestDomainEvent(IDomainEvent domainEvent)
        {
            AddDomainEvent(domainEvent);
        }

        public void AddTestDomainNotification(IDomainNotification domainNotification)
        {
            AddDomainNotification(domainNotification);
        }
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public string Message { get; }
        public TestDomainEvent(string message) => Message = message;
    }

    private sealed class TestDomainNotification : IDomainNotification
    {
        public string Message { get; }
        public TestDomainNotification(string message) => Message = message;
    }

    [Fact]
    public void AddDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("Test Event");

        // Act
        aggregate.AddTestDomainEvent(domainEvent);

        // Assert
        Assert.Single(aggregate.DomainEvents);
        Assert.Contains(domainEvent, aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_ShouldAddAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var event1 = new TestDomainEvent("Event 1");
        var event2 = new TestDomainEvent("Event 2");

        // Act
        aggregate.AddTestDomainEvent(event1);
        aggregate.AddTestDomainEvent(event2);

        // Assert
        Assert.Equal(2, aggregate.DomainEvents.Count);
        Assert.Contains(event1, aggregate.DomainEvents);
        Assert.Contains(event2, aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.AddTestDomainEvent(new TestDomainEvent("Event 1"));
        aggregate.AddTestDomainEvent(new TestDomainEvent("Event 2"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainNotification_ShouldAddNotificationToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var notification = new TestDomainNotification("Test Notification");

        // Act
        aggregate.AddTestDomainNotification(notification);

        // Assert
        Assert.Single(aggregate.DomainNotifications);
        Assert.Contains(notification, aggregate.DomainNotifications);
    }

    [Fact]
    public void AddDomainNotification_MultipleNotifications_ShouldAddAllNotifications()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var notification1 = new TestDomainNotification("Notification 1");
        var notification2 = new TestDomainNotification("Notification 2");

        // Act
        aggregate.AddTestDomainNotification(notification1);
        aggregate.AddTestDomainNotification(notification2);

        // Assert
        Assert.Equal(2, aggregate.DomainNotifications.Count);
        Assert.Contains(notification1, aggregate.DomainNotifications);
        Assert.Contains(notification2, aggregate.DomainNotifications);
    }

    [Fact]
    public void ClearDomainNotifications_ShouldRemoveAllNotifications()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.AddTestDomainNotification(new TestDomainNotification("Notification 1"));
        aggregate.AddTestDomainNotification(new TestDomainNotification("Notification 2"));

        // Act
        aggregate.ClearDomainNotifications();

        // Assert
        Assert.Empty(aggregate.DomainNotifications);
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.AddTestDomainEvent(new TestDomainEvent("Event"));

        // Act & Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<IDomainEvent>>(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainNotifications_ShouldBeReadOnly()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.AddTestDomainNotification(new TestDomainNotification("Notification"));

        // Act & Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<IDomainNotification>>(aggregate.DomainNotifications);
    }
}

