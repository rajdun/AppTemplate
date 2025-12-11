using Application.Common.ExtensionMethods;
using Application.Common.Mailing;
using Application.Common.Mailing.Templates;
using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public partial class UserRegisteredSendEmailNotificationHandler(ILogger<UserRegisteredSendEmailNotificationHandler> logger, IEmailService emailService)
    : IRequestHandler<UserRegistered>
{
    [LoggerMessage(0, LogLevel.Error, "[UserRegisteredEmail] Failed to send registration email to {Email}")]
    private static partial void LogEmailSendError(ILogger logger, string email, Exception ex);

    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var emailLanguage = AppLanguageHelpers.FromString(request.Language);

            await emailService.SendTemplatedEmailAsync(request.Email,
                new UserRegisteredEmailTemplate(request.Name, "Api", emailLanguage),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEmailSendError(logger, request.Email, ex);
            return Result.Fail(ex.Message);
        }

        return Result.Ok();
    }
}
