using Application.Common.Mediator;
using Application.Users.Dto;
using Application.Users.Interfaces;
using FluentResults;
using FluentValidation;

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

public class RegisterCommandHandler(IUserService userService) 
    : IRequestHandler<RegisterCommand, TokenResult>
{
    public async Task<Result<TokenResult>> Handle(RegisterCommand request, CancellationToken cancellationToken = new())
    {
        return await userService.RegisterAsync(request.Username, request.Email, request.Password, cancellationToken);
    }
}