using Api.Common;
using Carter;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCarter();
        services.AddOpenApi("internal");
        services.AddEndpointsApiExplorer();
        services.AddLocalization();
        
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddCors(configuration);
        
        return services;
    }

    public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
        
        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        
        return services;
    }
    
    private class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public string PolicyName { get; set; } = "DefaultCorsPolicy";
    }
}