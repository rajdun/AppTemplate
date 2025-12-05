namespace Application.Common.Dto;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; }
    public SortDirection SortOrder { get; set; } = SortDirection.Asc;

    public string? Query { get; set; }
    public ICollection<string> QueryColumns { get; } = [];

    public virtual string? GetActualSortField()
    {
#pragma warning disable CA1308
        return SortBy?.ToLowerInvariant();
#pragma warning restore CA1308
    }
}

public class StringFilterField
{
    public string? IsEqual { get; set; }
    public string? IsNotEqual { get; set; }
    public string? Contains { get; set; }
    public string? StartsWith { get; set; }

    // Disable warnings about mutable collections for filter fields as they may be set during deserialization
#pragma warning disable CA2227
    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<string>? InArray { get; set; }
#pragma warning restore CA2227
    // Disable warnings about mutable collections for filter fields as they may be set during deserialization
#pragma warning disable CA2227
    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<string>? NotInArray { get; set; }
#pragma warning restore CA2227
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
    // Disable warnings about mutable collections for filter fields as they may be set during deserialization
#pragma warning disable CA2227
    // ReSharper disable once UnusedMember.Global
    public ICollection<T>? InArray { get; set; }
#pragma warning restore CA2227

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
