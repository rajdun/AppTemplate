using Application.Common.Dto;

namespace Application.Common.Elasticsearch.Dto;

public class PagedUserRequest : PagedRequest
{
    public StringFilterField? Name { get; set; }
    public StringFilterField? Email { get; set; }

    public override string? GetActualSortField()
    {
#pragma warning disable CA1308
        return SortBy?.ToLowerInvariant() switch
#pragma warning restore CA1308
        {
            "name" => "name.keyword",
            "email" => "email.keyword",
            _ => base.GetActualSortField()
        };
    }
}
