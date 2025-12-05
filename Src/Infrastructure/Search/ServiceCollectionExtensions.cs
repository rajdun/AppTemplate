using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Search;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddMeilisearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MeilisearchSettings>(configuration.GetSection(MeilisearchSettings.SectionName));

        services.AddSingleton<MeilisearchClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MeilisearchSettings>>().Value;
            return new MeilisearchClient(settings.Uri, settings.ApiKey);
        });

        // Register generic search implementation for application-level ISearch<>
        services.AddScoped(typeof(Application.Common.Search.ISearch<>), typeof(MeiliSearch<>));

        return services;
    }
}
