using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meilisearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Search;

internal sealed partial class MeilisearchInitializer(
    MeilisearchClient client,
    IOptions<MeilisearchSettings> options,
    ILogger<MeilisearchInitializer> logger) : IHostedService
{
    private readonly MeilisearchClient _client = client;
    private readonly MeilisearchSettings _settings = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.Indexes.Count == 0)
        {
            LogNoIndexConfig();
            return;
        }

        foreach (var indexCfg in _settings.Indexes)
        {
            if (string.IsNullOrWhiteSpace(indexCfg.Name))
            {
                LogEmptyIndexName();
                continue;
            }

            try
            {
                // Ensure index exists
                Meilisearch.Index index;
                var primaryKey = string.IsNullOrWhiteSpace(indexCfg.PrimaryKey) ? "id" : indexCfg.PrimaryKey;
                try
                {
                    index = await _client.GetIndexAsync(indexCfg.Name, cancellationToken).ConfigureAwait(false);
                }
                catch (MeilisearchApiError)
                {
                    LogCreatingIndex(indexCfg.Name);
                    var createTask = await _client.CreateIndexAsync(indexCfg.Name, primaryKey: primaryKey, cancellationToken: cancellationToken).ConfigureAwait(false);
                    await _client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
                    index = _client.Index(indexCfg.Name);
                }

                // Apply settings (currently sortable + filterable attributes)
                var sortable = (indexCfg.SortableAttributes)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var filterable = (indexCfg.FilterableAttributes)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                Settings? currentSettings = null;
                var settingsToUpdate = new Settings();
                var shouldUpdate = false;

                if (sortable.Count > 0)
                {
                    currentSettings = await index.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
                    var currentSortable = currentSettings?.SortableAttributes ?? [];

                    var desiredSet = new HashSet<string>(sortable, StringComparer.Ordinal);
                    var currentSet = new HashSet<string>(currentSortable.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.Ordinal);

                    if (!desiredSet.SetEquals(currentSet))
                    {
                        LogUpdatingSortable(indexCfg.Name, string.Join(", ", sortable));
                        settingsToUpdate.SortableAttributes = sortable;
                        shouldUpdate = true;
                    }
                    else
                    {
                        LogSortableUnchanged(indexCfg.Name);
                    }
                }
                else
                {
                    LogNoSortable(indexCfg.Name);
                }

                if (filterable.Count > 0)
                {
                    currentSettings ??= await index.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
                    var currentFilterable = currentSettings?.FilterableAttributes ?? [];

                    var desiredSet = new HashSet<string>(filterable, StringComparer.Ordinal);
                    var currentSet = new HashSet<string>(currentFilterable.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.Ordinal);

                    if (!desiredSet.SetEquals(currentSet))
                    {
                        LogUpdatingFilterable(indexCfg.Name, string.Join(", ", filterable));
                        settingsToUpdate.FilterableAttributes = filterable;
                        shouldUpdate = true;
                    }
                    else
                    {
                        LogFilterableUnchanged(indexCfg.Name);
                    }
                }
                else
                {
                    LogNoFilterable(indexCfg.Name);
                }

                if (shouldUpdate)
                {
                    var task = await index.UpdateSettingsAsync(settingsToUpdate, cancellationToken).ConfigureAwait(false);
                    await _client.WaitForTaskAsync(task.TaskUid, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (MeilisearchApiError ex)
            {
                LogInitFailed(indexCfg.Name, ex.Message);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Debug, "[MeilisearchInitializer] No index configuration provided. Skipping settings initialization.")]
    private partial void LogNoIndexConfig();

    [LoggerMessage(LogLevel.Warning, "[MeilisearchInitializer] Encountered index configuration with empty Name. Skipping.")]
    private partial void LogEmptyIndexName();

    [LoggerMessage(LogLevel.Information, "[MeilisearchInitializer] Creating index '{indexName}'")]
    private partial void LogCreatingIndex(string indexName);

    [LoggerMessage(LogLevel.Information, "[MeilisearchInitializer] Updating sortable attributes for index '{indexName}': {attrs}")]
    private partial void LogUpdatingSortable(string indexName, string attrs);

    [LoggerMessage(LogLevel.Debug, "[MeilisearchInitializer] Sortable attributes already up to date for index '{indexName}'.")]
    private partial void LogSortableUnchanged(string indexName);

    [LoggerMessage(LogLevel.Debug, "[MeilisearchInitializer] No sortable attributes configured for index '{indexName}'.")]
    private partial void LogNoSortable(string indexName);

    [LoggerMessage(LogLevel.Information, "[MeilisearchInitializer] Updating filterable attributes for index '{indexName}': {attrs}")]
    private partial void LogUpdatingFilterable(string indexName, string attrs);

    [LoggerMessage(LogLevel.Debug, "[MeilisearchInitializer] Filterable attributes already up to date for index '{indexName}'.")]
    private partial void LogFilterableUnchanged(string indexName);

    [LoggerMessage(LogLevel.Debug, "[MeilisearchInitializer] No filterable attributes configured for index '{indexName}'.")]
    private partial void LogNoFilterable(string indexName);

    [LoggerMessage(LogLevel.Error, "[MeilisearchInitializer] Failed to initialize settings for index '{indexName}': {error}")]
    private partial void LogInitFailed(string indexName, string error);
}
