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
    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var smtpClient = new SmtpClient(settings.Value.Host, settings.Value.Port);
        smtpClient.EnableSsl = settings.Value.UseSsl;
        smtpClient.Credentials = string.IsNullOrEmpty(settings.Value.Username)
            ? null
            : new NetworkCredential(settings.Value.Username, settings.Value.Password);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(settings.Value.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }

    public async Task SendTemplatedEmailAsync(string to, EmailTemplate template, CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "Mailing",
            "Templates",
            template.Language.ToLanguageCode(),
            $"{template.TemplateName}.html"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found: {templatePath}");
        }

        var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var htmlBody = string.Format(templateContent, template.GetParameters());

        using var smtpClient = new SmtpClient(settings.Value.Host, settings.Value.Port);
        smtpClient.EnableSsl = settings.Value.UseSsl;
        smtpClient.Credentials = string.IsNullOrEmpty(settings.Value.Username)
            ? null
            : new NetworkCredential(settings.Value.Username, settings.Value.Password);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(settings.Value.From),
            Subject = template.Subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        // Add CC recipients
        foreach (var cc in template.Cc)
        {
            mailMessage.CC.Add(cc);
        }

        // Add BCC recipients
        foreach (var bcc in template.Bcc)
        {
            mailMessage.Bcc.Add(bcc);
        }

        // Set ReplyTo if provided
        if (!string.IsNullOrEmpty(template.ReplyTo))
        {
            mailMessage.ReplyToList.Add(template.ReplyTo);
        }

        // Add attachments
        foreach (var attachment in template.Attachments)
        {
            var stream = new MemoryStream(attachment.Content);
            var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
            mailMessage.Attachments.Add(mailAttachment);
        }

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
