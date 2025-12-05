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

    public string IndexName { get; } = typeof(T).Name.ToUpperInvariant();

    public async Task IndexAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var list = documents as IReadOnlyCollection<T> ?? documents.ToList();
        if (list.Count == 0)
        {
            LogNoDocumentsToIndex(IndexName);
            return;
        }

        LogIndexingDocuments(IndexName, list.Count);
        var index = _client.Index(IndexName);
        var taskInfo = await index.AddDocumentsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        LogWaitingForIndexTask(taskInfo.TaskUid);
        await _client.WaitForTaskAsync(taskInfo.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
        LogIndexingCompleted(IndexName, list.Count, taskInfo.TaskUid);
    }

    public async Task DeleteAsync(IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentIds);
        var ids = documentIds.Select(x => x.ToString()).ToList();
        if (ids.Count == 0)
        {
            LogNoDocumentsToDelete(IndexName);
            return;
        }

        LogDeletingDocuments(IndexName, ids.Count);
        var index = _client.Index(IndexName);
        var taskInfo = await index.DeleteDocumentsAsync(ids, cancellationToken).ConfigureAwait(false);
        LogWaitingForDeleteTask(taskInfo.TaskUid);
        await _client.WaitForTaskAsync(taskInfo.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
        LogDeletionCompleted(IndexName, ids.Count, taskInfo.TaskUid);
    }

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] No documents to index for index '{IndexName}'")]
    partial void LogNoDocumentsToIndex(string indexName);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Indexing {Count} document(s) to index '{IndexName}'")]
    partial void LogIndexingDocuments(string indexName, int count);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Waiting for index task {TaskUid} to complete")]
    partial void LogWaitingForIndexTask(int taskUid);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Indexed {Count} document(s) to index '{IndexName}' (TaskUid: {TaskUid})")]
    partial void LogIndexingCompleted(string indexName, int count, int taskUid);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] No documents to delete from index '{IndexName}'")]
    partial void LogNoDocumentsToDelete(string indexName);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Deleting {Count} document(s) from index '{IndexName}'")]
    partial void LogDeletingDocuments(string indexName, int count);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Waiting for delete task {TaskUid} to complete")]
    partial void LogWaitingForDeleteTask(int taskUid);

    [LoggerMessage(LogLevel.Debug, "[MeiliSearch] Deleted {Count} document(s) from index '{IndexName}' (TaskUid: {TaskUid})")]
    partial void LogDeletionCompleted(string indexName, int count, int taskUid);
}
