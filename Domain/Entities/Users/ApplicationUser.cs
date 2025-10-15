using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Users;

[Table("Users", Schema = "Users")]
public class ApplicationUser : IdentityUser<Guid>
{
}

public class ApplicationRole : IdentityRole<Guid>
{
}