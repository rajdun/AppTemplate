using Microsoft.Extensions.Configuration;

namespace Infrastructure.Search;

internal sealed class MeilisearchSettings
{
    public const string SectionName = "MeilisearchSettings";

    // Uri of the Meilisearch server, e.g., http://localhost:7700
    public string Uri { get; init; } = string.Empty;

    // API key (master or search/admin, depending on needs)
    public string ApiKey { get; init; } = string.Empty;
}
