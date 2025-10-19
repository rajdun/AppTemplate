using Hangfire.Dashboard;

namespace Api.Common;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return context.GetHttpContext().User.Identity?.IsAuthenticated ?? false;
    }
}