using Application.Common.Mailing;
using Application.Users.NotificationHandlers;
using Domain.Aggregates.Identity.DomainNotifications;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ApplicationTests.Users.NotificationHandlers;

public class UserRegisteredSendEmailNotificationHandlerTests
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegisteredSendEmailNotificationHandler> _logger;

    public UserRegisteredSendEmailNotificationHandlerTests()
    {
        _emailService = Substitute.For<IEmailService>();
        _logger = Substitute.For<ILogger<UserRegisteredSendEmailNotificationHandler>>();
    }

    [Fact]
    public async Task Handle_WithValidNotification_ShouldSendTemplatedEmail()
    {
        // Arrange
        var notification = new UserRegistered(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", "pl");
        var handler = new UserRegisteredSendEmailNotificationHandler(_logger, _emailService);

        // Act
        var result = await handler.Handle(notification);

        // Assert
        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendTemplatedEmailAsync(
            "jan@example.com",
            Arg.Any<EmailTemplate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrows_ShouldReturnFail()
    {
        // Arrange
        var notification = new UserRegistered(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", "pl");
        _emailService.SendTemplatedEmailAsync(Arg.Any<string>(), Arg.Any<EmailTemplate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var handler = new UserRegisteredSendEmailNotificationHandler(_logger, _emailService);

        // Act
        var result = await handler.Handle(notification);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("SMTP unavailable", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEnglishLanguage_ShouldStillSendEmail()
    {
        // Arrange
        var notification = new UserRegistered(Guid.NewGuid(), "John Doe", "john@example.com", "en");
        var handler = new UserRegisteredSendEmailNotificationHandler(_logger, _emailService);

        // Act
        var result = await handler.Handle(notification);

        // Assert
        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendTemplatedEmailAsync(
            "john@example.com",
            Arg.Any<EmailTemplate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new UserRegisteredSendEmailNotificationHandler(_logger, _emailService);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));
    }
}

