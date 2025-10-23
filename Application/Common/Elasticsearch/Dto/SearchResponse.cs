namespace Application.Common.Elasticsearch.Dto;

/// <summary>
/// Obiekt przechowujący wyniki wyszukiwania wraz z informacjami o paginacji.
/// </summary>
/// <typeparam name="T">Typ zwracanego dokumentu</typeparam>
public class SearchResult<T> where T : class
{
    /// <summary>
    /// Łączna liczba wszystkich pasujących dokumentów (niezależnie od strony).
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Dokumenty znalezione na bieżącej stronie.
    /// </summary>
    public IEnumerable<T> Documents { get; set; } = Enumerable.Empty<T>();

    // Informacje zwrotne o paginacji
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}