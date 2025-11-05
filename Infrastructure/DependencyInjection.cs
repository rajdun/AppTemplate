using System.Text;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Mailing;
using Application.Common.Messaging;
using Domain.Entities.Users;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Infrastructure.Data;
using Infrastructure.Elasticsearch;
using Infrastructure.Implementation;
using Infrastructure.Implementation.Dto;
using Infrastructure.Mailing;
using Infrastructure.Mailing.Dto;
using Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using StackExchange.Redis;

namespace Infrastructure;

public static class DependencyInjection
{
    private const string RedisConnectionStringName = "Redis";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddIdentity(configuration);
        services.AddImplementation(configuration);
        services.AddCache(configuration);
        services.AddHealthChecks(configuration);
        services.AddHangfire(configuration);
        services.AddMailing(configuration);
        services.AddElasticsearch(configuration);

        return services;
    }

    public static IHostApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
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

        builder.Host.UseSerilog();

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
    
    private static IServiceCollection AddMailing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
    
    private static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(RedisConnectionStringName) ?? "localhost:6379";

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(connectionString));

        return services;
    }

    private static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);
        if (string.IsNullOrWhiteSpace(redisConnectionString)) return services;

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddImplementation(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUser>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            return CurrentUser.CreateAsync(httpContextAccessor, cacheService).GetAwaiter().GetResult();
        });
        services.AddScoped<IHangfireJobExecutor, HangfireJobExecutor>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddAuthorization();
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var key = configuration["JwtSettings:Secret"];

                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException("JWTSettings:Secret", "JWT Secret is not configured.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };
            });

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddSingleton<DomainEventToOutboxInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<DomainEventToOutboxInterceptor>();
            options.UseNpgsql(connectionString)
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName) ?? "";

        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        healthChecks.AddRedis(redisConnectionString, RedisConnectionStringName);

        return services;
    }
}