using Application.Common.Interfaces;
using Application.Resources;
using Application.Users.Dto;
using Domain.Aggregates.Identity;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Application.Users.Commands;

public record LoginCommand(string Username, string Password) : IRequest<TokenResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

internal sealed class LoginCommandHandler(
    UserManager<User> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    ICacheService cacheService)
    : IRequestHandler<LoginCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken = new())
    {
        var user = await userManager.FindByNameAsync(request.Username).ConfigureAwait(false);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
        {
            return Result.Fail<TokenResult>(UserTranslations.InvalidPasswordOrUsername);
        }

        if (user.DeactivatedAt is not null)
        {
            return Result.Fail(UserTranslations.UserNotActive);
        }

        var token = await jwtTokenGenerator.GenerateToken(user).ConfigureAwait(false);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenKey = CacheKeys.GetRefreshTokenCacheKey(user.Id.ToString());

        await cacheService.SetAsync(refreshTokenKey, refreshToken, TimeSpan.FromDays(7), cancellationToken).ConfigureAwait(false);

        return Result.Ok(new TokenResult(token, refreshToken));
    }
}
