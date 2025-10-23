using System;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Elasticsearch
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind options (section "Elasticsearch")
            var options = new ElasticSearchOptions();
            configuration.GetSection("Elasticsearch").Bind(options);

            // Register options
            services.AddSingleton(options);

            // Create and register ElasticsearchClient
            var settings = new ElasticsearchClientSettings(new Uri(options.Uri));
            var client = new ElasticsearchClient(settings);
            services.AddSingleton(client);

            // Register service wrapper
            services.AddSingleton<IElasticSearch, ElasticSearch>();

            return services;
        }
    }
}

