using System.Globalization;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

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

    internal static IHostApplicationBuilder AddTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = builder.Configuration["OTEL_SERVICE_NAME"] ?? "unknown-service"
                };
            })
            .CreateLogger();

        builder.Services.AddSerilog();

        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "my-api-service";
        var serviceVersion = builder.Configuration["OTEL_SERVICE_VERSION"] ?? "1.0.0";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(opt => { opt.Protocol = OtlpExportProtocol.HttpProtobuf; }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => { opt.Protocol = OtlpExportProtocol.HttpProtobuf; }));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return builder;
    }
}
