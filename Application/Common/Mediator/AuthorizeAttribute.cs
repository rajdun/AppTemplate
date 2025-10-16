using Application.Common.Interfaces;

namespace Application.Common.Mediator;

[AttributeUsage(AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
    public AuthorizePolicy AuthorizeUserBehaviour { get; }

    public AuthorizeAttribute(AuthorizePolicy policy)
    {
        AuthorizeUserBehaviour = policy;
    }
    
    public bool Authorize(IUser user)
    {
        if(!user.IsAuthenticated)
            return false;
        
        if(AuthorizeUserBehaviour.HasFlag(AuthorizePolicy.Any))
            return true;
        
        if(AuthorizeUserBehaviour.HasFlag(AuthorizePolicy.Admin) && user.IsAdmin)
            return true;
        
        return false;
    }
}