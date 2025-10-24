namespace Application.Common.Elasticsearch.Dto;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public virtual string? SortBy { get; set; }
    public SortDirection SortOrder { get; set; } = SortDirection.Asc;
    
    public string? Query { get; set; }
    public ICollection<string> QueryColumns { get; set; } = [];
}

public class StringFilterField
{
    public string? IsEqual { get; set; }
    public string? IsNotEqual { get; set; }
    public string? Contains { get; set; }
    public string? StartsWith { get; set; }
    public List<string>? InArray { get; set; }
    public List<string>? NotInArray { get; set; }
    public bool? IsNull { get; set; }
}

public class RangeFilterField<T> where T : struct
{
    public T? IsEqual { get; set; }
    public T? IsNotEqual { get; set; }
    public T? IsGreaterThan { get; set; }
    public T? IsGreaterThanOrEqual { get; set; }
    public T? IsLessThan { get; set; }
    public T? IsLessThanOrEqual { get; set; }
    public List<T>? InArray { get; set; }
    
    public T? RangeStart { get; set; }
    public T? RangeEnd { get; set; }
}

public class BoolFilterField
{
    public bool? Is { get; set; }
}

public enum SortDirection
{
    Asc,
    Desc
}
