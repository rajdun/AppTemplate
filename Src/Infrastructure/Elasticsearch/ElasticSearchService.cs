using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using SearchRequest = Elastic.Clients.Elasticsearch.SearchRequest;
using SortOrder = Elastic.Clients.Elasticsearch.SortOrder;

namespace Infrastructure.Elasticsearch;

internal class ElasticSearchService<T> : IElasticSearchService<T> where T : class, IElasticDocument
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchService<T>> _logger;
    private readonly string _indexName;

    public ElasticSearchService(
        ElasticsearchClient client,
        ILogger<ElasticSearchService<T>> logger)
    {
        _client = client;
        _logger = logger;

        _indexName = typeof(T).Name.ToLowerInvariant();
    }


    public async Task<bool> IndexDocumentAsync(T document)
    {
        var response = await _client.IndexAsync(document, req => req
            .Index(_indexName)
            .Id(document.Id)
        );

        if (!response.IsSuccess())
        {
            _logger.LogError("An error has occured while indexing document {Id} in index {Index}: {Error}", document.Id,
                _indexName, response.DebugInformation);
            return false;
        }

        return true;
    }

    public async Task<T?> GetDocumentAsync(string id)
    {
        var response = await _client.GetAsync<T>(id, cfg => cfg.Index(_indexName));
        if (!response.IsSuccess())
        {
            _logger.LogWarning("Could not retrieve document {Id} from index {Index}: {Error}", id, _indexName,
                response.DebugInformation);
            return null;
        }

        return response.Source;
    }

    public async Task<bool> DeleteDocumentAsync(string id)
    {
        var response = await _client.DeleteAsync(_indexName, id);
        if (!response.IsSuccess())
        {
            _logger.LogError("An error has occured while deleting document {Id} from index {Index}: {Error}", id,
                _indexName, response.DebugInformation);
            return false;
        }

        return true;
    }
}
