using Hangfire.Dashboard;

namespace Api.Common;

public class HangfireAuthorizationFilter(IConfiguration configuration)
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (configuration.GetValue<bool>("Hangfire:AllowAnonymousDashboardAccess"))
        {
            return true;
        }

        var httpContext = context.GetHttpContext();
        var isAuthorized = httpContext.User.Identity?.IsAuthenticated ?? false;

        return isAuthorized;
    }
}
