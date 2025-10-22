using Application.Common.ExtensionMethods;
using Application.Common.Mailing;
using Application.Common.Mailing.Templates;
using Application.Common.ValueObjects;
using Domain.Common;
using Domain.DomainEvents.User;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger, IEmailService emailService)
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