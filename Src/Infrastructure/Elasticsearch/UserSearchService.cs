using Application.Common.Dto;
using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Infrastructure.Elasticsearch.Extensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Elasticsearch;

public class UserSearchService : IUserSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<UserSearchService> _logger;
    private readonly string _indexName;

    public UserSearchService(
        ElasticsearchClient client,
        ILogger<UserSearchService> logger)
    {
        _client = client;
        _logger = logger;

        _indexName = nameof(ElasticUser).ToLowerInvariant();
    }

    public async Task<PagedResult<ElasticUser>> SearchUsersAsync(PagedUserRequest request, CancellationToken cancellationToken = new())
    {
        // 1. Pagination
        var from = (request.PageNumber - 1) * request.PageSize;
        var size = request.PageSize;

        // 2. Sorting
        var sortOptions = new List<SortOptions>();
        sortOptions.AddSortOptions(request);

        // 3. Query construction
        var mustQueries = new List<Query>();
        var filterQueries = new List<Query>();

        // 3a. Text quey
        mustQueries.AddQueryFilters(request);

        // 3b. Structured filters
        filterQueries.AddStringFilter("name", request.Name);
        filterQueries.AddStringFilter("email", request.Email);

        // If no must queries, add match_all
        if (mustQueries.Count == 0)
        {
            mustQueries.Add(new Query { MatchAll = new MatchAllQuery() });
        }

        // 4. Execute search
        try
        {
            var searchResponse = await _client.SearchAsync<ElasticUser>(s => s
                .Indices(_indexName)
                .From(from)
                .Size(size)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustQueries)
                        .Filter(filterQueries)
                    )
                )
                .Sort(sortOptions), cancellationToken);

            if (!searchResponse.IsValidResponse)
            {
                var error = searchResponse.ElasticsearchServerError?.ToString() ?? searchResponse.DebugInformation;
                _logger.LogError("An error has occured during executing elasticsearch query: {Error}", error);

                return new PagedResult<ElasticUser>
                {
                    Items = new List<ElasticUser>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }

            // 5. Return paged result
            return new PagedResult<ElasticUser>
            {
                Items = searchResponse.Documents.ToList(),
                TotalCount = searchResponse.Total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error has occured during executing elasticsearch query on index: {IndexName}", _indexName);

            return new PagedResult<ElasticUser>
            {
                Items = new List<ElasticUser>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
