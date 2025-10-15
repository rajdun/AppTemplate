using Application.Users.Dto;
using Domain.Entities.Users;
using FluentResults;

namespace Application.Users.Interfaces;

public interface IUserService
{
    Task<Result<TokenResult>> LoginAsync(string username, string password, CancellationToken cancellationToken = new());
    Task<Result<TokenResult>> RegisterAsync(string username, string? email, string password,
        CancellationToken cancellationToken = new());
}