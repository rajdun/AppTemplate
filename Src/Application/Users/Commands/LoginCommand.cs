using Application.Common.Interfaces;
using Application.Users.Dto;
using Application.Users.ExtensionMethods;
using Application.Users.Interfaces;
using Domain.Common.Interfaces;
using FluentResults;
using FluentValidation;

namespace Application.Users.Commands;

public record LoginCommand(string Email, string Password) : IRequest<TokenResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

internal class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    ICacheService cacheService)
    : IRequestHandler<LoginCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await identityService.ValidateCredentialsAsync(request.Email, request.Password)
            .ConfigureAwait(false);

        if (validationResult.IsFailed)
        {
            return Result.Fail<TokenResult>(validationResult.Errors);
        }

        var userId = validationResult.Value;

        var userProfileResult = await identityService.GetUserProfileAsync(userId).ConfigureAwait(false);
        if (userProfileResult.IsFailed)
        {
            return Result.Fail<TokenResult>(userProfileResult.Errors);
        }

        var userProfile = userProfileResult.Value;

        var token = jwtTokenGenerator.GenerateToken(userId, userProfile.FirstName, userProfile.LastName);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await cacheService.SaveRefreshTokenAsync(userId, refreshToken, TimeSpan.FromHours(8), cancellationToken).ConfigureAwait(false);

        return Result.Ok(new TokenResult(token, refreshToken));
    }
}
