using System;
using Application.Common.Elasticsearch;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Elasticsearch;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ElasticsearchSettings>(
            configuration.GetSection(ElasticsearchSettings.SectionName)
        );
        
        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;
            
            var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Uri))
                .DefaultIndex(settings.DefaultIndex);
            
            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                clientSettings.Authentication(
                    new BasicAuthentication(settings.Username, settings.Password)
                );
            }
            
#if DEBUG
            clientSettings.DisableDirectStreaming()
                .EnableDebugMode();
#endif


            return new ElasticsearchClient(clientSettings);
        });

        services.AddScoped(typeof(IElasticSearchService<>), typeof(ElasticSearchService<>));
        
        services.AddScoped<IUserSearchService, UserSearchService>();
        
        return services;
    }
}

internal class ElasticsearchSettings
{
    public string Uri { get; set; } = string.Empty;
    public string DefaultIndex { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public static string SectionName => "ElasticsearchSettings";
}