namespace Application.Common.Elasticsearch.Dto;

public class PagedUserRequest : PagedRequest
{
    private string? _sortBy;
    public StringFilterField? Name { get; set; }
    public StringFilterField? Email { get; set; }

    public override string? SortBy
    {
        get
        {
            if (_sortBy == null)
            {
                return null;
            }
            return $"{_sortBy.ToLowerInvariant()}.keyword";
        }
        set => _sortBy = value;
    }
}