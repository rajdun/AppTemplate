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

public record RegisterCommand(string Username, string? Email, string Password, string RepeatPassword) : IRequest<TokenResult>;

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

public class RegisterCommandHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator, IStringLocalizer<UserTranslations> localizer)
    : IRequestHandler<RegisterCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RegisterCommand request, CancellationToken cancellationToken = new())
    {
        var existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return Result.Fail(localizer["UsernameAlreadyExists"]);
        }
        
        var newUser = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email
        };
        
        var createResult = await userManager.CreateAsync(newUser, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Fail(errors);
        }
        
        var token = await jwtTokenGenerator.GenerateToken(newUser);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        
        return Result.Ok(new TokenResult(token, refreshToken));
    }
}