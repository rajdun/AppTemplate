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

internal class DeactivateUserCommandHandler(UserManager<ApplicationUser> userManager, ILogger<DeactivateUserCommandHandler> logger)
    : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    public async Task<Result<DeactivateUserResult>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            logger.LogError("User with ID {UserId} not found for deactivation", request.UserId);
            return Result.Fail(UserTranslations.UserNotFound);
        }

        if (user.DeactivatedAt is not null)
        {
            logger.LogWarning("User with ID {UserId} is already deactivated", request.UserId);
            return Result.Fail(UserTranslations.UserNotActive);
        }

        user.DeactivatedAt = DateTimeOffset.UtcNow;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to deactivate user with ID {UserId}. Errors: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Fail(result.Errors.Select(x => x.Description));
        }

        return Result.Ok(new DeactivateUserResult(user.Id, user.UserName, user.Email));
    }
}
