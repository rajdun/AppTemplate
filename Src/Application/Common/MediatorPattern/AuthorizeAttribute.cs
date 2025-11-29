using Application.Common.Interfaces;

namespace Application.Common.MediatorPattern;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthorizeAttribute : Attribute
{
    public AuthorizeAttribute(AuthorizePolicy policy)
    {
        Policy = policy;
    }

    public AuthorizePolicy Policy { get; }

    public bool Authorize(IUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsAuthenticated)
        {
            return false;
        }

        if (Policy == AuthorizePolicy.None)
        {
            return true;
        }

        if (Policy.HasFlag(AuthorizePolicy.Admin) && user.IsAdmin)
        {
            return true;
        }

        return false;
    }
}
