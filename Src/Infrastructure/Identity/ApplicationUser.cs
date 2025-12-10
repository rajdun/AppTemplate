using Domain.Aggregates.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public UserProfile DomainUserProfile { get; set; } = null!;
}
