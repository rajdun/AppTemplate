using Hangfire.Dashboard;

namespace Api.Common;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Implement proper authorization logic here. For now, allow all access.
        return true;
    }
}