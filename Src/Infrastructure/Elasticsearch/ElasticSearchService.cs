using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using SearchRequest = Elastic.Clients.Elasticsearch.SearchRequest;
using SortOrder = Elastic.Clients.Elasticsearch.SortOrder;

namespace Infrastructure.Elasticsearch;

internal partial class ElasticSearchService<T> : IElasticSearchService<T> where T : class, IElasticDocument
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchService<T>> _logger;
    private readonly string _indexName;

    [LoggerMessage(LogLevel.Error, "An error has occured while indexing document {Id} in index {Index}: {Error}")]
    private static partial void LogIndexingError(ILogger logger, string id, string index, string error);

    [LoggerMessage(LogLevel.Warning, "Could not retrieve document {Id} from index {Index}: {Error}")]
    private static partial void LogRetrievalWarning(ILogger logger, string id, string index, string error);

    [LoggerMessage(LogLevel.Error, "An error has occured while deleting document {Id} from index {Index}: {Error}")]
    private static partial void LogDeletionError(ILogger logger, string id, string index, string error);

    public ElasticSearchService(
        ElasticsearchClient client,
        ILogger<ElasticSearchService<T>> logger)
    {
        _client = client;
        _logger = logger;

        _indexName = typeof(T).Name.ToUpperInvariant();
    }


    public async Task<bool> IndexDocumentAsync(T document)
    {
        var response = await _client.IndexAsync(document, req => req
            .Index(_indexName)
            .Id(document.Id)
        ).ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            LogIndexingError(_logger, document.Id, _indexName, response.DebugInformation);
            return false;
        }

        return true;
    }

    public async Task<T?> GetDocumentAsync(string id)
    {
        var response = await _client.GetAsync<T>(id, cfg => cfg.Index(_indexName)).ConfigureAwait(false);
        if (!response.IsSuccess())
        {
            LogRetrievalWarning(_logger, id, _indexName, response.DebugInformation);
            return null;
        }

        return response.Source;
    }

    public async Task<bool> DeleteDocumentAsync(string id)
    {
        var response = await _client.DeleteAsync(_indexName, id).ConfigureAwait(false);
        if (!response.IsSuccess())
        {
            LogDeletionError(_logger, id, _indexName, response.DebugInformation);
            return false;
        }

        return true;
    }
}
