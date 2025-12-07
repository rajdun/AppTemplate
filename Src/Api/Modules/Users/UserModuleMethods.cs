using Api.Common;
using Api.Modules.Users.Requests;
using Application.Common.Dto;
using Application.Common.MediatorPattern;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Application.Users.Commands;
using Application.Users.Dto;
using Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = Api.Modules.Users.Requests.LoginRequest;
using RegisterRequest = Api.Modules.Users.Requests.RegisterRequest;

namespace Api.Modules.Users;

#pragma warning disable CA1515
public sealed partial class UsersModule
#pragma warning restore CA1515
{
    private static async Task<IResult> Login([FromServices] IMediator mediator, [FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Login, request.Password);

        var response = await mediator.SendAsync<LoginCommand, TokenResult>(command).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    private static async Task<IResult> Register([FromServices] IMediator mediator, [FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Username, request.Email, request.Password, request.RepeatPassword);

        var response = await mediator.SendAsync<RegisterCommand, TokenResult>(command).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    private static async Task<IResult> RefreshToken([FromServices] IMediator mediator,
        [FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var response = await mediator.SendAsync<RefreshTokenCommand, TokenResult>(command).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    private static async Task<IResult> SearchUsers([FromServices] IMediator mediator,
        [FromBody] PagedUserRequest request, CancellationToken cancellationToken = default)
    {
        var query = new SearchUsersQuery(request);

        var response = await mediator.SendAsync<SearchUsersQuery, PagedResult<UserSearchDocumentDto>>(query, cancellationToken).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    private static async Task<IResult> DeactivateUser([FromServices] IMediator mediator, [FromRoute] Guid userId,
        CancellationToken cancellationToken = new())
    {
        var command = new DeactivateUserCommand(userId);

        var response = await mediator.SendAsync<DeactivateUserCommand, DeactivateUserResult>(command, cancellationToken).ConfigureAwait(false);

        return response.ToHttpResult();
    }
}
