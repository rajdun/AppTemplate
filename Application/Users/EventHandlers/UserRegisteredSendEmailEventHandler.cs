using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Application.Common.ExtensionMethods;
using Application.Common.Mailing;
using Application.Common.Mailing.Templates;
using Application.Common.ValueObjects;
using Domain.Common;
using Domain.DomainNotifications.User;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class UserRegisteredSendEmailEventHandler(ILogger<UserRegisteredSendEmailEventHandler> logger, IEmailService emailService)
    : IRequestHandler<UserRegistered>
{
    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new())
    {
        try
        {
            var emailLanguage = AppLanguageHelpers.FromString(request.Language);
            
            await emailService.SendTemplatedEmailAsync(request.Email,
                new UserRegisteredEmailTemplate(request.Name, "Api", emailLanguage),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send UserRegistered email to {Email}", request.Email);
            return Result.Fail(ex.Message);
        }

        return Result.Ok();
    }
}