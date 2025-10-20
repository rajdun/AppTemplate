using Hangfire.Dashboard;

namespace Api.Common;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
#if DEBUG
        return true;
#else
        return context.GetHttpContext().User.Identity?.IsAuthenticated ?? false;
#endif
    }
}