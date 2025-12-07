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

public record RegisterCommand(string Username, string? Email, string Password, string RepeatPassword)
    : IRequest<TokenResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty();

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.RepeatPassword)
            .Equal(x => x.Password);
    }
}

public class RegisterCommandHandler(
    UserManager<User> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    ICacheService cacheService,
    IUser user)
    : IRequestHandler<RegisterCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RegisterCommand request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingUser = await userManager.FindByNameAsync(request.Username).ConfigureAwait(false);
        if (existingUser != null)
        {
            return Result.Fail<TokenResult>(UserTranslations.UsernameAlreadyExists);
        }

        var newUser = new User(request.Username, request.Email, user.Language.ToString());

        var createResult = await userManager.CreateAsync(newUser, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description).ToList();
            return Result.Fail<TokenResult>(errors);
        }

        var token = await jwtTokenGenerator.GenerateToken(newUser).ConfigureAwait(false);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenKey = CacheKeys.GetRefreshTokenCacheKey(newUser.Id.ToString());
        await cacheService.SetAsync(refreshTokenKey, refreshToken, TimeSpan.FromDays(7), cancellationToken).ConfigureAwait(false);

        return Result.Ok(new TokenResult(token, refreshToken));
    }
}
