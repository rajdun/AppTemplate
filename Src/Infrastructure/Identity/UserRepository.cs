using Application.UserProfiles.Interfaces;
using Domain.Aggregates.Identity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class UserProfileRepository : IUserProfileProfileRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.Id == id)
            .Select(x=>x.DomainUserProfile)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.Email == email)
            .Select(x=>x.DomainUserProfile)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<UserProfile>().AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<UserProfile>().Update(user);
        return Task.CompletedTask;
    }
}
