using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Meilisearch;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Search;

internal partial class UserSearch(ILogger<UserSearch> logger, MeilisearchClient meilisearchClient)
    : IUserSearch
{
    private readonly MeilisearchClient _client = meilisearchClient;
    private readonly string _indexName = nameof(UserSearchDocumentDto).ToUpperInvariant();

    public async Task<PagedResult<UserSearchDocumentDto>> SearchUsersAsync(PagedUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        LogSearchingUsers(request.Query, request.PageNumber, request.PageSize, request.SortBy, request.SortOrder.ToString());

        var index = _client.Index(_indexName);

        var filters = new List<string>();
        // Only support equality and IN filters for Meilisearch filter syntax; others will be ignored.
        if (request.Name is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Name.IsEqual))
            {
                filters.Add($"name = '{EscapeFilterValue(request.Name.IsEqual)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Name.IsNotEqual))
            {
                filters.Add($"name != '{EscapeFilterValue(request.Name.IsNotEqual)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Name.Contains))
            {
                filters.Add($"name CONTAINS '{EscapeFilterValue(request.Name.Contains)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Name.StartsWith))
            {
                filters.Add($"name STARTS WITH '{EscapeFilterValue(request.Name.StartsWith)}'");
            }

            if (request.Name.InArray is { Count: > 0 })
            {
                var nonEmptyValues = request.Name.InArray.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                if (nonEmptyValues.Count > 0)
                {
                    filters.Add($"name IN [{string.Join(", ", nonEmptyValues.Select(v => $"'{EscapeFilterValue(v)}'"))}]");
                }
            }

            if (request.Name.NotInArray is { Count: > 0 })
            {
                var nonEmptyValues = request.Name.NotInArray.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                if (nonEmptyValues.Count > 0)
                {
                    filters.Add($"name NOT IN [{string.Join(", ", nonEmptyValues.Select(v => $"'{EscapeFilterValue(v)}'"))}]");
                }
            }

            if (request.Name.IsNull is not null)
            {
                filters.Add(request.Name.IsNull.Value ? "name IS NULL" : "name IS NOT NULL");
            }
        }
        if (request.Email is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Email.IsEqual))
            {
                filters.Add($"email = '{EscapeFilterValue(request.Email.IsEqual)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Email.IsNotEqual))
            {
                filters.Add($"email != '{EscapeFilterValue(request.Email.IsNotEqual)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Email.Contains))
            {
                filters.Add($"email CONTAINS '{EscapeFilterValue(request.Email.Contains)}'");
            }

            if (!string.IsNullOrWhiteSpace(request.Email.StartsWith))
            {
                filters.Add($"email STARTS WITH '{EscapeFilterValue(request.Email.StartsWith)}'");
            }

            if (request.Email.InArray is { Count: > 0 })
            {
                var nonEmptyValues = request.Email.InArray.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                if (nonEmptyValues.Count > 0)
                {
                    filters.Add($"email IN [{string.Join(", ", nonEmptyValues.Select(v => $"'{EscapeFilterValue(v)}'"))}]");
                }
            }

            if (request.Email.NotInArray is { Count: > 0 })
            {
                var nonEmptyValues = request.Email.NotInArray.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                if (nonEmptyValues.Count > 0)
                {
                    filters.Add($"email NOT IN [{string.Join(", ", nonEmptyValues.Select(v => $"'{EscapeFilterValue(v)}'"))}]");
                }
            }

            if (request.Email.IsNull is not null)
            {
                filters.Add(request.Email.IsNull.Value ? "email IS NULL" : "email IS NOT NULL");
            }
        }

        var sortList = new List<string>();
        var sortField = request.GetActualSortField();
        if (!string.IsNullOrWhiteSpace(sortField))
        {
            var order = request.SortOrder == SortDirection.Asc ? "asc" : "desc";
            sortList.Add($"{sortField}:{order}");
        }

        var searchParams = new SearchQuery
        {
            Q = request.Query,
            Limit = request.PageSize,
            Offset = (request.PageNumber - 1) * request.PageSize,
            Filter = filters.Count > 0 ? string.Join(" AND ", filters) : null,
            Sort = sortList.Count > 0 ? sortList : null
        };

        LogBuiltQuery(_indexName, searchParams.Filter ?? string.Empty, sortList.Count > 0 ? string.Join(",", sortList) : string.Empty, searchParams.Offset ?? 0, searchParams.Limit ?? 0);

        var result = await index.SearchAsync<UserSearchDocumentDto>(request.Query ?? string.Empty, searchParams, cancellationToken).ConfigureAwait(false);

        var items = result.Hits ?? new List<UserSearchDocumentDto>();
        var total = (long)items.Count;

        LogSearchCompleted(items.Count, total);

        return new PagedResult<UserSearchDocumentDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }



    private static string EscapeFilterValue(string value)
        => value.Replace("'", "\\'", StringComparison.Ordinal);

    [LoggerMessage(LogLevel.Debug, "[UserSearch] Searching users. Query='{Query}', Page={Page}, Size={Size}, SortBy='{SortBy}', SortOrder={SortOrder}")]
    private partial void LogSearchingUsers(string? query, int page, int size, string? sortBy, string? sortOrder);

    [LoggerMessage(LogLevel.Debug, "[UserSearch] Built Meilisearch query. Index='{Index}', Filter='{Filter}', Sort='{Sort}', Offset={Offset}, Limit={Limit}")]
    private partial void LogBuiltQuery(string index, string filter, string sort, int offset, int limit);

    [LoggerMessage(LogLevel.Debug, "[UserSearch] Search completed. Retrieved {Count} item(s). Estimated total: {Total}")]
    private partial void LogSearchCompleted(int count, long total);
}
