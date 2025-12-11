using Application.Users.Dto;
using Application.Users.Interfaces;
using Domain.Common.Interfaces;
using FluentResults;
using FluentValidation;

namespace Application.Users.Commands;

public record DeactivateUserCommand(Guid UserId) : IRequest<DeactivateUserResult>;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

internal class DeactivateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    public async Task<Result<DeactivateUserResult>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken = default)
    {
        var userProfileResult = await identityService.GetUserProfileAsync(request.UserId).ConfigureAwait(false);
        if (userProfileResult.IsFailed)
        {
            return Result.Fail<DeactivateUserResult>(userProfileResult.Errors);
        }

        var userProfile = userProfileResult.Value;
        var fullName = $"{userProfile.FirstName} {userProfile.LastName}".Trim();

        var deactivateResult = await identityService.DeactivateUserAsync(request.UserId).ConfigureAwait(false);
        if (deactivateResult.IsFailed)
        {
            return Result.Fail<DeactivateUserResult>(deactivateResult.Errors);
        }

        return Result.Ok(new DeactivateUserResult(request.UserId, fullName, userProfile.Email));
    }
}

