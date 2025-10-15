using Application.Common.Mediator;
using Application.Users.Dto;
using Application.Users.Interfaces;
using FluentResults;
using FluentValidation;

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

internal sealed class LoginCommandHandler(IUserService userService) 
    : IRequestHandler<LoginCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken = new())
    {
        return await userService.LoginAsync(request.Username, request.Password, cancellationToken);
    }
}

