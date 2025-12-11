using Application.Common;
using Application.Common.Interfaces;
using Application.Users.Interfaces;
using Domain.Aggregates.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.Users
            .Include(u => u.DomainUserProfile)
            .FirstOrDefaultAsync(u => u.Email == email)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Fail<Guid>("Invalid email or password");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
        if (!isPasswordValid)
        {
            return Result.Fail<Guid>("Invalid email or password");
        }

        if (user.DomainUserProfile is null || !user.DomainUserProfile.CanLogin())
        {
            return Result.Fail<Guid>("User account is deactivated");
        }

        return Result.Ok(user.Id);
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, string firstName, string lastName)
    {
        var existingUser = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existingUser is not null)
        {
            return Result.Fail<Guid>("User with this email already exists");
        }

        var userId = Guid.NewGuid();

        // Create domain user profile
        var userProfile = UserProfile.Create(userId, firstName, lastName, email);

        // Create identity user
        var applicationUser = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DomainUserProfile = userProfile
        };

        var result = await _userManager.CreateAsync(applicationUser, password).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Fail<Guid>(errors);
        }

        return Result.Ok(userId);
    }

    public async Task<Result<UserProfile>> GetUserProfileAsync(Guid userId)
    {
        var user = await _userManager.Users
            .Include(u => u.DomainUserProfile)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);

        if (user?.DomainUserProfile is null)
        {
            return Result.Fail<UserProfile>("User not found");
        }

        return Result.Ok(user.DomainUserProfile);
    }

    public async Task<Result> DeactivateUserAsync(Guid userId)
    {
        var user = await _userManager.Users
            .Include(u => u.DomainUserProfile)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);

        if (user?.DomainUserProfile is null)
        {
            return Result.Fail("User not found");
        }

        if (!user.DomainUserProfile.CanLogin())
        {
            return Result.Fail("User is already deactivated");
        }

        user.DomainUserProfile.Deactivate(_dateTimeProvider.UtcNow);

        await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<Result> UpdateUserAsync(Guid userId, string firstName, string lastName)
    {
        var user = await _userManager.Users
            .Include(u => u.DomainUserProfile)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);

        if (user?.DomainUserProfile is null)
        {
            return Result.Fail("User not found");
        }

        user.DomainUserProfile.Update(firstName, lastName);

        await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Fail("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Fail(errors);
        }

        return Result.Ok();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        return user is not null;
    }
}
