using Domain.Aggregates.Identity;

namespace Application.Users.Interfaces;

public interface IUserProfileRepository
{
    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    public Task AddAsync(UserProfile user, CancellationToken cancellationToken = default);
    public Task UpdateAsync(UserProfile user, CancellationToken cancellationToken = default);
}
