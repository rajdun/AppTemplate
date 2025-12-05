using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Users.Queries;
using Meilisearch;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Search;

internal partial class MeiliSearch<T>(MeilisearchClient client, ILogger<MeiliSearch<T>> logger) : ISearch<T>
    where T : SearchDocumentDto
{
    private readonly MeilisearchClient _client = client;

    public string IndexName { get; } = typeof(T).Name;

    public async Task IndexAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var list = documents as IReadOnlyCollection<T> ?? documents.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var index = _client.Index(IndexName);
        var taskInfo = await index.AddDocumentsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        await _client.WaitForTaskAsync(taskInfo.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentIds);
        var ids = documentIds.Select(x => x.ToString()).ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var index = _client.Index(IndexName);
        var taskInfo = await index.DeleteDocumentsAsync(ids, cancellationToken).ConfigureAwait(false);
        await _client.WaitForTaskAsync(taskInfo.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // Note: Generic search is not ideal for user-specific query shapes. Prefer a dedicated service.
    public async Task<PagedResult<T>> SearchAsync(SearchUsersQuery request, CancellationToken cancellationToken = default)
    {
        // Best-effort implementation using only term + pagination + simple sort if present.
        var index = _client.Index(IndexName);

        // Derive offset/limit from request if available; otherwise apply defaults.
        var pageNumber = request.Request.PageNumber < 1 ? 1 : request.Request.PageNumber;
        var pageSize = request.Request.PageSize < 1 ? 1 : request.Request.PageSize;
        var offset = (pageNumber - 1) * pageSize;
        var limit = pageSize;

        var sort = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Request.SortBy))
        {
            var dir = request.Request.SortOrder.ToString().Equals("Asc", StringComparison.OrdinalIgnoreCase)
                ? "asc"
                : "desc";
            sort.Add(request.Request.SortBy + ":" + dir);
        }

        var q = request.Request.Query ?? string.Empty;

        var options = new Meilisearch.SearchQuery
        {
            Offset = offset,
            Limit = limit,
            Sort = sort.Count > 0 ? sort : null
        };

        try
        {
            var res = await index.SearchAsync<T>(q, options, cancellationToken).ConfigureAwait(false);
            var items = res.Hits?.ToList() ?? new List<T>();
            var total = items.Count;
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (MeilisearchApiError)
        {
            LogMeilisearchQueryFailedForIndexIndex(IndexName);
            return new PagedResult<T>
            {
                Items = Array.Empty<T>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    [LoggerMessage(LogLevel.Error, "Meilisearch query failed for index {Index}")]
    partial void LogMeilisearchQueryFailedForIndexIndex(string Index);
}
