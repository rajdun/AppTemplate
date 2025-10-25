namespace Application.Common.Elasticsearch.Dto;

public class PagedUserRequest : PagedRequest
{
    public StringFilterField? Name { get; set; }
    public StringFilterField? Email { get; set; }

    public override string? GetActualSortField()
    {
        return this.SortBy?.ToLowerInvariant() switch
        {
            "name" => "name.keyword",
            "email" => "email.keyword",
            _ => base.GetActualSortField()
        };
    }
}