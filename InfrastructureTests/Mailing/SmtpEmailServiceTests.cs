using Application.Common.Mailing;
using Application.Common.ValueObjects;
using Infrastructure.Mailing;
using Infrastructure.Mailing.Dto;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InfrastructureTests.Mailing;

public class SmtpEmailServiceTests
{
    private readonly IOptions<SmtpSettings> _settings;
    private readonly SmtpSettings _smtpSettings;

    public SmtpEmailServiceTests()
    {
        _smtpSettings = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            UseSsl = true,
            Username = "test@test.com",
            Password = "password",
            From = "noreply@test.com"
        };
        _settings = Substitute.For<IOptions<SmtpSettings>>();
        _settings.Value.Returns(_smtpSettings);
    }

    [Fact]
    public void SmtpEmailService_ShouldInitializeWithSettings()
    {
        // Act
        var service = new SmtpEmailService(_settings);

        // Assert
        Assert.NotNull(service);
    }

    // Note: Full integration tests for SMTP would require a test SMTP server
    // These tests verify the service can be instantiated with various configurations

    [Fact]
    public void SmtpEmailService_WithoutCredentials_ShouldInitialize()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 25,
            UseSsl = false,
            Username = null,
            Password = null,
            From = "noreply@test.com"
        };
        var options = Substitute.For<IOptions<SmtpSettings>>();
        options.Value.Returns(settings);

        // Act
        var service = new SmtpEmailService(options);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void SmtpEmailService_WithSslEnabled_ShouldInitialize()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 465,
            UseSsl = true,
            Username = "user@gmail.com",
            Password = "password",
            From = "user@gmail.com"
        };
        var options = Substitute.For<IOptions<SmtpSettings>>();
        options.Value.Returns(settings);

        // Act
        var service = new SmtpEmailService(options);

        // Assert
        Assert.NotNull(service);
    }

    private class TestEmailTemplate : EmailTemplate
    {
        public override string TemplateName => "TestTemplate";
        public override string Subject => "Test Subject";
        public override object[] GetParameters() => new object[] { "John", "Welcome" };
    }

    [Fact]
    public void EmailTemplate_Properties_ShouldBeAccessible()
    {
        // Arrange & Act
        var template = new TestEmailTemplate
        {
            Language = AppLanguage.En,
            Cc = new List<string> { "cc@test.com" },
            Bcc = new List<string> { "bcc@test.com" },
            ReplyTo = "reply@test.com",
            Attachments = new List<EmailAttachment>
            {
                new("test.pdf", new byte[] { 1, 2, 3 }, "application/pdf")
            }
        };

        // Assert
        Assert.Equal("TestTemplate", template.TemplateName);
        Assert.Equal("Test Subject", template.Subject);
        Assert.Equal(AppLanguage.En, template.Language);
        Assert.Single(template.Cc);
        Assert.Single(template.Bcc);
        Assert.Equal("reply@test.com", template.ReplyTo);
        Assert.Single(template.Attachments);
        Assert.Equal(2, template.GetParameters().Length);
    }
}

