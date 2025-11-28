namespace Application.Common.Mailing;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    public Task SendTemplatedEmailAsync(string to, EmailTemplate template, CancellationToken cancellationToken = default);
}
