using Application.Common.Mailing;
using Application.Common.Mailing.Templates;
using Application.Common.ValueObjects;

namespace InfrastructureTests.Mailing;

public class EmailTemplateTests
{
    [Fact]
    public void UserRegisteredEmailTemplate_ShouldHaveCorrectProperties()
    {
        // Arrange
        var userName = "TestUser";
        var appName = "TestApp";
        var language = AppLanguage.En;

        // Act
        var template = new UserRegisteredEmailTemplate(userName, appName, language);

        // Assert
        Assert.Equal("UserRegistered", template.TemplateName);
        Assert.Equal(language, template.Language);
        Assert.NotNull(template.Subject);
        Assert.NotEmpty(template.Subject);
    }

    [Fact]
    public void UserRegisteredEmailTemplate_GetParameters_ShouldReturnCorrectParameters()
    {
        // Arrange
        var userName = "TestUser";
        var appName = "TestApp";
        var template = new UserRegisteredEmailTemplate(userName, appName, AppLanguage.En);

        // Act
        var parameters = template.GetParameters();

        // Assert
        Assert.NotNull(parameters);
        Assert.Equal(2, parameters.Length);
        Assert.Equal(userName, parameters[0]);
        Assert.Equal(appName, parameters[1]);
    }

    [Fact]
    public void UserRegisteredEmailTemplate_WithPolishLanguage_ShouldUsePolishSubject()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.Pl);

        // Act
        var subject = template.Subject;

        // Assert
        Assert.NotNull(subject);
        Assert.NotEmpty(subject);
    }

    [Fact]
    public void UserRegisteredEmailTemplate_WithEnglishLanguage_ShouldUseEnglishSubject()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);

        // Act
        var subject = template.Subject;

        // Assert
        Assert.NotNull(subject);
        Assert.NotEmpty(subject);
    }

    [Fact]
    public void EmailTemplate_DefaultCc_ShouldBeEmpty()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);

        // Act
        var cc = template.Cc;

        // Assert
        Assert.NotNull(cc);
        Assert.Empty(cc);
    }

    [Fact]
    public void EmailTemplate_DefaultBcc_ShouldBeEmpty()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);

        // Act
        var bcc = template.Bcc;

        // Assert
        Assert.NotNull(bcc);
        Assert.Empty(bcc);
    }

    [Fact]
    public void EmailTemplate_DefaultReplyTo_ShouldBeNull()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);

        // Act
        var replyTo = template.ReplyTo;

        // Assert
        Assert.Null(replyTo);
    }

    [Fact]
    public void EmailTemplate_DefaultAttachments_ShouldBeEmpty()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);

        // Act
        var attachments = template.Attachments;

        // Assert
        Assert.NotNull(attachments);
        Assert.Empty(attachments);
    }

    [Fact]
    public void EmailTemplate_CanAddCcRecipients()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);
        var ccEmail = "cc@test.com";

        // Act
        template.Cc.Add(ccEmail);

        // Assert
        Assert.Single(template.Cc);
        Assert.Contains(ccEmail, template.Cc);
    }

    [Fact]
    public void EmailTemplate_CanAddBccRecipients()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);
        var bccEmail = "bcc@test.com";

        // Act
        template.Bcc.Add(bccEmail);

        // Assert
        Assert.Single(template.Bcc);
        Assert.Contains(bccEmail, template.Bcc);
    }

    [Fact]
    public void EmailTemplate_CanAddAttachments()
    {
        // Arrange
        var template = new UserRegisteredEmailTemplate("User", "App", AppLanguage.En);
        var attachment = new EmailAttachment("test.pdf", new byte[] { 1, 2, 3 }, "application/pdf");

        // Act
        template.Attachments.Add(attachment);

        // Assert
        Assert.Single(template.Attachments);
        Assert.Equal(attachment.FileName, template.Attachments.First().FileName);
    }
}


