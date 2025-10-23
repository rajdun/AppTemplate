using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;

namespace Infrastructure.Elasticsearch
{
    public interface IElasticSearch
    {
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
        Task EnsureIndexAsync(string indexName, CancellationToken cancellationToken = default);
        Task IndexAsync<T>(string indexName, string id, T document, CancellationToken cancellationToken = default);
        Task BulkIndexAsync<T>(string indexName, IEnumerable<T> documents, Func<T, string>? idSelector = null, CancellationToken cancellationToken = default);
    }

    public class ElasticSearch : IElasticSearch
    {
        private readonly ElasticsearchClient _client;

        // Use DI to inject an ElasticsearchClient; if you need to construct manually you can use the other ctor.
        public ElasticSearch(ElasticsearchClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // Alternative ctor for quick POC usage
        public ElasticSearch(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) uri = "http://localhost:9200";
            var options = new ElasticsearchClientSettings(new Uri(uri));
            _client = new ElasticsearchClient(options);
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.PingAsync(cancellationToken).ConfigureAwait(false);
                return response.IsValidResponse;
            }
            catch
            {
                return false;
            }
        }

        public async Task EnsureIndexAsync(string indexName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));
            var exists = await _client.Indices.ExistsAsync(indexName,cancellationToken).ConfigureAwait(false);
            if (exists.Exists) return;

            // Create index with default settings; you can extend to add mappings
            await _client.Indices.CreateAsync(indexName, cancellationToken).ConfigureAwait(false);
        }

        public async Task IndexAsync<T>(string indexName, string id, T document, CancellationToken cancellationToken = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            await EnsureIndexAsync(indexName, cancellationToken).ConfigureAwait(false);
            await _client.IndexAsync(document, i => i.Index(indexName).Id(id), cancellationToken).ConfigureAwait(false);
        }

        public async Task BulkIndexAsync<T>(string indexName, IEnumerable<T> documents, Func<T, string>? idSelector = null, CancellationToken cancellationToken = default)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            var docs = documents.ToList();
            if (!docs.Any()) return;

            await EnsureIndexAsync(indexName, cancellationToken).ConfigureAwait(false);

            var operations = new List<BulkOperation>();
            foreach (var doc in docs)
            {
                if (idSelector != null)
                {
                    var id = idSelector(doc);
                    operations.Add(new BulkIndexOperation<T>(doc) { Id = id });
                }
                else
                {
                    operations.Add(new BulkIndexOperation<T>(doc));
                }
            }

            var bulkResponse = await _client.BulkAsync(b => b.Index(indexName), cancellationToken).ConfigureAwait(false);
            
            if (!bulkResponse.IsValidResponse)
            {
                throw new Exception($"Bulk index failed: {bulkResponse?.ItemsWithErrors.Select(x=>x.Error?.Reason).FirstOrDefault()}");
            }
        }
    }
}