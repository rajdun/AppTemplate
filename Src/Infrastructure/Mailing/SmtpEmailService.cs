using System.Globalization;
using Application.Common.Mailing;
using Infrastructure.Mailing.Dto;
using System.Net;
using System.Net.Mail;
using Application.Common.ExtensionMethods;
using Microsoft.Extensions.Options;

namespace Infrastructure.Mailing;

internal class SmtpEmailService(IOptions<SmtpSettings> settings)
    : IEmailService
{
    public async Task SendEmailAsync(string sendTo, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var smtpClient = new SmtpClient(settings.Value.Host, settings.Value.Port);
        smtpClient.EnableSsl = settings.Value.UseSsl;
        smtpClient.Credentials = string.IsNullOrEmpty(settings.Value.Username)
            ? null
            : new NetworkCredential(settings.Value.Username, settings.Value.Password);

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(settings.Value.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(sendTo);

        await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendTemplatedEmailAsync(string sendTo, EmailTemplate emailTemplate, CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "Mailing",
            "Templates",
            emailTemplate.Language.ToLanguageCode(),
            $"{emailTemplate.TemplateName}.html"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found: {templatePath}");
        }

        var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken).ConfigureAwait(false);
        var htmlBody = string.Format(CultureInfo.InvariantCulture, templateContent, emailTemplate.GetParameters());

        using var smtpClient = new SmtpClient(settings.Value.Host, settings.Value.Port);
        smtpClient.EnableSsl = settings.Value.UseSsl;
        smtpClient.Credentials = string.IsNullOrEmpty(settings.Value.Username)
            ? null
            : new NetworkCredential(settings.Value.Username, settings.Value.Password);

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(settings.Value.From),
            Subject = emailTemplate.Subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(sendTo);

        // Add CC recipients
        foreach (var cc in emailTemplate.Cc)
        {
            mailMessage.CC.Add(cc);
        }

        // Add BCC recipients
        foreach (var bcc in emailTemplate.Bcc)
        {
            mailMessage.Bcc.Add(bcc);
        }

        // Set ReplyTo if provided
        if (!string.IsNullOrEmpty(emailTemplate.ReplyTo))
        {
            mailMessage.ReplyToList.Add(emailTemplate.ReplyTo);
        }

        // Add attachments
        foreach (var attachment in emailTemplate.Attachments)
        {
            var stream = new MemoryStream(attachment.Content);
            var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
            mailMessage.Attachments.Add(mailAttachment);
        }

        await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
    }
}
