using Application.Common.ExtensionMethods;
using Application.Common.Mailing;
using Application.Common.ValueObjects;

namespace ApplicationTests.Common.Mailing;

public class EmailTemplateTests
{
    private class TestEmailTemplate : EmailTemplate
    {
        public override string TemplateName => "TestTemplate";
        public override string Subject => "Test Subject";
        public override object[] GetParameters() => new object[] { "param1", "param2" };
    }

    [Fact]
    public void EmailTemplate_ShouldHaveDefaultLanguageAsPl()
    {
        // Arrange & Act
        var template = new TestEmailTemplate();

        // Assert
        Assert.Equal(AppLanguage.Pl, template.Language);
    }

    [Fact]
    public void EmailTemplate_ShouldAllowLanguageOverride()
    {
        // Arrange & Act
        var template = new TestEmailTemplate { Language = AppLanguage.En };

        // Assert
        Assert.Equal(AppLanguage.En, template.Language);
    }

    [Fact]
    public void EmailTemplate_ShouldHaveEmptyAttachmentsByDefault()
    {
        // Arrange & Act
        var template = new TestEmailTemplate();

        // Assert
        Assert.NotNull(template.Attachments);
        Assert.Empty(template.Attachments);
    }

    [Fact]
    public void EmailTemplate_ShouldHaveEmptyCcByDefault()
    {
        // Arrange & Act
        var template = new TestEmailTemplate();

        // Assert
        Assert.NotNull(template.Cc);
        Assert.Empty(template.Cc);
    }

    [Fact]
    public void EmailTemplate_ShouldHaveEmptyBccByDefault()
    {
        // Arrange & Act
        var template = new TestEmailTemplate();

        // Assert
        Assert.NotNull(template.Bcc);
        Assert.Empty(template.Bcc);
    }

    [Fact]
    public void EmailTemplate_ShouldHaveNullReplyToByDefault()
    {
        // Arrange & Act
        var template = new TestEmailTemplate();

        // Assert
        Assert.Null(template.ReplyTo);
    }

    [Fact]
    public void EmailTemplate_ShouldAllowAddingAttachments()
    {
        // Arrange
        var attachment = new EmailAttachment("test.pdf", new byte[] { 1, 2, 3 }, "application/pdf");

        // Act
        var template = new TestEmailTemplate
        {
            Attachments = new List<EmailAttachment> { attachment }
        };

        // Assert
        Assert.Single(template.Attachments);
        Assert.Equal("test.pdf", template.Attachments.First().FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, template.Attachments.First().Content);
        Assert.Equal("application/pdf", template.Attachments.First().ContentType);
    }

    [Fact]
    public void EmailTemplate_ShouldAllowAddingCcRecipients()
    {
        // Arrange & Act
        var template = new TestEmailTemplate
        {
            Cc = new List<string> { "cc1@example.com", "cc2@example.com" }
        };

        // Assert
        Assert.Equal(2, template.Cc.Count);
        Assert.Contains("cc1@example.com", template.Cc);
        Assert.Contains("cc2@example.com", template.Cc);
    }

    [Fact]
    public void EmailTemplate_ShouldAllowAddingBccRecipients()
    {
        // Arrange & Act
        var template = new TestEmailTemplate
        {
            Bcc = new List<string> { "bcc1@example.com", "bcc2@example.com" }
        };

        // Assert
        Assert.Equal(2, template.Bcc.Count);
        Assert.Contains("bcc1@example.com", template.Bcc);
        Assert.Contains("bcc2@example.com", template.Bcc);
    }

    [Fact]
    public void EmailTemplate_ShouldAllowSettingReplyTo()
    {
        // Arrange & Act
        var template = new TestEmailTemplate
        {
            ReplyTo = "reply@example.com"
        };

        // Assert
        Assert.Equal("reply@example.com", template.ReplyTo);
    }

    [Fact]
    public void EmailAttachment_ShouldCreateWithAllProperties()
    {
        // Arrange
        var fileName = "document.pdf";
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var contentType = "application/pdf";

        // Act
        var attachment = new EmailAttachment(fileName, content, contentType);

        // Assert
        Assert.Equal(fileName, attachment.FileName);
        Assert.Equal(content, attachment.Content);
        Assert.Equal(contentType, attachment.ContentType);
    }
}

public class AppLanguageHelpersTests
{
    [Theory]
    [InlineData(AppLanguage.Pl, "pl")]
    [InlineData(AppLanguage.En, "en")]
    public void ToLanguageCode_ShouldReturnCorrectCode(AppLanguage language, string expectedCode)
    {
        // Act
        var result = language.ToLanguageCode();

        // Assert
        Assert.Equal(expectedCode, result);
    }

    [Theory]
    [InlineData("pl", AppLanguage.Pl)]
    [InlineData("PL", AppLanguage.Pl)]
    [InlineData("en", AppLanguage.En)]
    [InlineData("EN", AppLanguage.En)]
    [InlineData("fr", AppLanguage.En)] // Default case
    [InlineData("", AppLanguage.En)] // Default case
    public void FromString_ShouldReturnCorrectLanguage(string languageCode, AppLanguage expected)
    {
        // Act
        var result = AppLanguageHelpers.FromString(languageCode);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromString_WithNull_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => AppLanguageHelpers.FromString(null!));
    }
}

