using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Resources;
using Application.Users.Dto;
using Domain.Common;
using Domain.Entities.Users;
using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Application.Users.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResult>;

[Authorize(AuthorizePolicy.None)]
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

internal class RefreshTokenCommandHandler(
    ICacheService cacheService,
    IJwtTokenGenerator jwtTokenGenerator,
    UserManager<ApplicationUser> userManager,
    IUser currentUser,
    IStringLocalizer<UserTranslations> localizer)
    : IRequestHandler<RefreshTokenCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken = new())
    {
        var refreshToken = CacheKeys.GetRefreshTokenCacheKey(currentUser.UserId.ToString());

        var cachedToken = await cacheService.GetAsync<string>(refreshToken, cancellationToken);
        if (cachedToken == null || cachedToken != request.RefreshToken)
            return Result.Fail(localizer["InvalidRefreshToken"]);

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user == null) return Result.Fail(localizer["InvalidRefreshToken"]);

        var newJwtToken = await jwtTokenGenerator.GenerateToken(user);
        var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await cacheService.SetAsync(refreshToken, newRefreshToken, TimeSpan.FromDays(7), cancellationToken);

        return Result.Ok(new TokenResult(newJwtToken, newRefreshToken));
    }
}