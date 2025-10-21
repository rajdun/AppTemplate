namespace Application.Common.Mailing;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendTemplatedEmailAsync(string to, EmailTemplate template, CancellationToken cancellationToken = default);
}