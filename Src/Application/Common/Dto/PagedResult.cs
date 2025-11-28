namespace Application.Common.Dto;

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = new List<T>();
    public long TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
