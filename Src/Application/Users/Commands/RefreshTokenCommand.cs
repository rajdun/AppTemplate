using Application.Common.Interfaces;
using Application.Users.Dto;
using Application.Users.ExtensionMethods;
using Application.Users.Interfaces;
using Domain.Common.Interfaces;
using FluentResults;
using FluentValidation;

namespace Application.Users.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResult>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

internal class RefreshTokenCommandHandler(IUser currentUser, IJwtTokenGenerator jwtTokenGenerator, ICacheService cacheService, IIdentityService identityService)
    : IRequestHandler<RefreshTokenCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken = default)
    {
        var cachedTokenResult = await cacheService.GetRefreshTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);

        if (cachedTokenResult == null || currentUser.UserId != cachedTokenResult.Value)
        {
            return Result.Fail<TokenResult>("Invalid refresh token");
        }

        var userProfile = await identityService.GetUserProfileAsync(currentUser.UserId).ConfigureAwait(false);

        var token = jwtTokenGenerator.GenerateToken(currentUser.UserId, userProfile.Value.FirstName, userProfile.Value.LastName);
        var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await cacheService.SaveRefreshTokenAsync(currentUser.UserId, newRefreshToken, TimeSpan.FromHours(8), cancellationToken).ConfigureAwait(false);

        return Result.Ok(new TokenResult(token, newRefreshToken));
    }
}

