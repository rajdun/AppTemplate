namespace Application.Common.Mailing;

public interface IEmailService
{
    public Task SendEmailAsync(string sendTo, string subject, string htmlBody, CancellationToken cancellationToken = default);
    public Task SendTemplatedEmailAsync(string sendTo, EmailTemplate emailTemplate, CancellationToken cancellationToken = default);
}
