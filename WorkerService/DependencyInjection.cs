using Hangfire;
using Hangfire.Redis.StackExchange;

namespace WorkerService;

internal static class DependencyInjection
{
    internal static IServiceCollection AddWorkerService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization();
        
        // ILocalization does not work without this.
        services.AddRouting();
        services.AddHangfireServer();
        
        return services;
    }
}