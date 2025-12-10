using FluentResults;

namespace Application.Users.Interfaces;

public interface IIdentityService
{
    public Task<Result<Guid>> ValidateCredentialsAsync(string email, string password);

    public Task<Result<Guid>> CreateUserAsync(string email, string password, string firstName, string lastName);
}
