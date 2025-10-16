using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Resources;
using Application.Users.Dto;
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

internal sealed class LoginCommandHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator, IStringLocalizer<UserTranslations> localizer) 
    : IRequestHandler<LoginCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken = new())
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return Result.Fail(localizer["UserNotFound"]);
        }
        
        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result.Fail(localizer["InvalidPassword"]);
        }
        
        var token = await jwtTokenGenerator.GenerateToken(user);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        
        return Result.Ok(new TokenResult(token, refreshToken));
    }
}

