using Application.Resources;
using Application.Users.Dto;
using Domain.Common;
using Domain.Entities.Users;
using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Users.Commands;

public record DeactivateUserCommand(Guid UserId) : IRequest<DeactivateUserResult>;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal partial class DeactivateUserCommandHandler(UserManager<ApplicationUser> userManager, ILogger<DeactivateUserCommandHandler> logger)
    : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    [LoggerMessage(Level = LogLevel.Error, Message = "User with ID {UserId} not found for deactivation")]
    private static partial void LogUserNotFound(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID {UserId} is already deactivated")]
    private static partial void LogUserAlreadyDeactivated(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deactivate user with ID {UserId}. Errors: {Errors}")]
    private static partial void LogDeactivationFailed(ILogger logger, Guid userId, string errors);

    public async Task<Result<DeactivateUserResult>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);

        if (user == null)
        {
            LogUserNotFound(logger, request.UserId);
            return Result.Fail(UserTranslations.UserNotFound);
        }

        if (user.DeactivatedAt is not null)
        {
            LogUserAlreadyDeactivated(logger, request.UserId);
            return Result.Fail(UserTranslations.UserNotActive);
        }

        user.DeactivatedAt = DateTimeOffset.UtcNow;

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            LogDeactivationFailed(logger, request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Fail(result.Errors.Select(x => x.Description));
        }

        return Result.Ok(new DeactivateUserResult(user.Id, user.UserName, user.Email));
    }
}
