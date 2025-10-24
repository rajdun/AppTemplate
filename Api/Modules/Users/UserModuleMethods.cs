using Api.Common;
using Api.Modules.Users.Requests;
using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;
using Application.Common.Mediator;
using Application.Users.Commands;
using Application.Users.Dto;
using Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = Api.Modules.Users.Requests.LoginRequest;
using RegisterRequest = Api.Modules.Users.Requests.RegisterRequest;

namespace Api.Modules.Users;

public partial class UsersModule
{
    private static async Task<IResult> Login([FromServices] IMediator mediator, [FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Login, request.Password);

        var response = await mediator.SendAsync<LoginCommand, TokenResult>(command);

        return response.ToHttpResult();
    }

    private static async Task<IResult> Register([FromServices] IMediator mediator, [FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Username, request.Email, request.Password, request.RepeatPassword);

        var response = await mediator.SendAsync<RegisterCommand, TokenResult>(command);

        return response.ToHttpResult();
    }

    private static async Task<IResult> RefreshToken([FromServices] IMediator mediator,
        [FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var response = await mediator.SendAsync<RefreshTokenCommand, TokenResult>(command);

        return response.ToHttpResult();
    }
    
    private static async Task<IResult> SearchUsers([FromServices] IMediator mediator,
        [FromBody] PagedUserRequest request, CancellationToken cancellationToken = new())
    {
        var query = new SearchUsersQuery(request);

        var response = await mediator.SendAsync<SearchUsersQuery, PagedResult<ElasticUser>>(query, cancellationToken);

        return response.ToHttpResult();
    }
}