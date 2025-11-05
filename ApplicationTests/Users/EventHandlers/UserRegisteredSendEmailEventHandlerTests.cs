using Application.Common.Mailing;
using Application.Common.Mailing.Templates;
using Application.Common.ValueObjects;
using Application.Users.EventHandlers;
using Domain.DomainEvents.User;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ApplicationTests.Users.EventHandlers;

public class UserRegisteredSendEmailEventHandlerTests
{
    private readonly ILogger<UserRegisteredSendEmailEventHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly UserRegisteredSendEmailEventHandler _handler;

    public UserRegisteredSendEmailEventHandlerTests()
    {
        _logger = Substitute.For<ILogger<UserRegisteredSendEmailEventHandler>>();
        _emailService = Substitute.For<IEmailService>();
        _handler = new UserRegisteredSendEmailEventHandler(_logger, _emailService);
    }

    [Fact]
    public async Task Handle_WhenEmailSentSuccessfully_ShouldReturnSuccess()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendTemplatedEmailAsync(
            domainEvent.Email,
            Arg.Is<UserRegisteredEmailTemplate>(t => 
                t.Language == AppLanguage.En &&
                t.TemplateName == "UserRegistered"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPolishLanguage_ShouldSendPolishEmail()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "pl");

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendTemplatedEmailAsync(
            domainEvent.Email,
            Arg.Is<UserRegisteredEmailTemplate>(t => t.Language == AppLanguage.Pl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        var exception = new Exception("SMTP connection failed");
        
        _emailService.SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<EmailTemplate>(),
            Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message == exception.Message);
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrowsException_ShouldLogError()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "en");
        var exception = new Exception("SMTP connection failed");
        
        _emailService.SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<EmailTemplate>(),
            Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(domainEvent.Email)),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WithInvalidLanguageCode_ShouldStillProcessWithDefaultLanguage()
    {
        // Arrange
        var domainEvent = new UserRegistered("testuser", "test@test.com", "invalid");

        // Act
        var result = await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<UserRegisteredEmailTemplate>(),
            Arg.Any<CancellationToken>());
    }
}

