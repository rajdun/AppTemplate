using Microsoft.Extensions.Configuration;

namespace Infrastructure.Search;

internal sealed class MeilisearchSettings
{
    public const string SectionName = "MeilisearchSettings";

    // Uri of the Meilisearch server, e.g., http://localhost:7700
    public string Uri { get; init; } = string.Empty;

    // API key (master or search/admin, depending on needs)
    public string ApiKey { get; init; } = string.Empty;

    // Optional per-index configuration applied at startup
    public List<MeilisearchIndexConfig> Indexes { get; init; } = [];
}

internal sealed class MeilisearchIndexConfig
{
    // Index UID (use same convention as in code, e.g., typeof(Dto).Name.ToUpperInvariant())
    public string Name { get; init; } = string.Empty;

    // Primary key to set when creating an index. Defaults to "id" when not provided.
    public string PrimaryKey { get; init; } = "id";

    // List of fields that can be used in sort parameter
    public List<string> SortableAttributes { get; init; } = [];

    // List of fields that can be used in filter parameter
    public List<string> FilterableAttributes { get; init; } = [];
}
