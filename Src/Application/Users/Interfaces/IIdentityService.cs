using Domain.Aggregates.Identity;
using FluentResults;

namespace Application.Users.Interfaces;

public interface IIdentityService
{
    public Task<Result<Guid>> ValidateCredentialsAsync(string email, string password);

    public Task<Result<Guid>> CreateUserAsync(string email, string password, string firstName, string lastName);

    public Task<Result<UserProfile>> GetUserProfileAsync(Guid userId);

    public Task<Result> DeactivateUserAsync(Guid userId);

    public Task<Result> UpdateUserAsync(Guid userId, string firstName, string lastName);

    public Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    public Task<bool> UserExistsAsync(string email);
}
