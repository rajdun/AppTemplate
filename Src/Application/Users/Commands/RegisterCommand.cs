using Application.Common.Interfaces;
using Application.Users.Dto;
using Application.Users.ExtensionMethods;
using Application.Users.Interfaces;
using Domain.Common.Interfaces;
using FluentResults;
using FluentValidation;

namespace Application.Users.Commands;

public record RegisterCommand(string Username, string Email, string Password, string RepeatPassword) : IRequest<TokenResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.RepeatPassword)
            .NotEmpty()
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match");
    }
}

internal class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    ICacheService cacheService)
    : IRequestHandler<RegisterCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        // Split username into first and last name (or use username as first name)
        var nameParts = request.Username.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var createResult = await identityService.CreateUserAsync(
            request.Email,
            request.Password,
            firstName,
            lastName).ConfigureAwait(false);

        if (createResult.IsFailed)
        {
            return Result.Fail<TokenResult>(createResult.Errors);
        }

        var userId = createResult.Value;

        var token = jwtTokenGenerator.GenerateToken(userId, firstName, lastName);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await cacheService.SaveRefreshTokenAsync(userId, refreshToken, TimeSpan.FromHours(8), cancellationToken).ConfigureAwait(false);

        return Result.Ok(new TokenResult(token, refreshToken));
    }
}

