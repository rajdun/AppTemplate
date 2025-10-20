using Application.Common.Interfaces;
using Application.Resources;
using Application.Users.Dto;
using Domain.Common;
using Domain.Entities.Users;
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
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    IStringLocalizer<UserTranslations> localizer,
    ICacheService cacheService)
    : IRequestHandler<LoginCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken = new())
    {
        var user = await userManager.FindByNameAsync(request.Username);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Result.Fail<TokenResult>(localizer["InvalidUsernameOrPassword"]);

        var token = await jwtTokenGenerator.GenerateToken(user);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenKey = CacheKeys.GetRefreshTokenCacheKey(user.Id.ToString());

        await cacheService.SetAsync(refreshTokenKey, refreshToken, TimeSpan.FromDays(7), cancellationToken);

        return Result.Ok(new TokenResult(token, refreshToken));
    }
}