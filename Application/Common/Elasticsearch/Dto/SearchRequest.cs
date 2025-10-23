using System.ComponentModel.DataAnnotations;

namespace Application.Common.Elasticsearch.Dto;

/// <summary>
/// Obiekt definiujący parametry wyszukiwania.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Tekst do wyszukania.
    /// </summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>
    /// Numer strony (zaczynając od 1).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Liczba wyników na stronie.
    /// </summary>
    [Range(1, 1000)] // Ustawiamy rozsądny limit
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Lista pól, w których ma się odbyć wyszukiwanie (np. "name", "description").
    /// Zastępuje niebezpieczne "Fields("*")".
    /// </summary>
    public IEnumerable<string> FieldsToSearch { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Pole, po którym ma nastąpić sortowanie.
    /// Jeśli puste, sortujemy po trafności (_score).
    /// </summary>
    public string SortField { get; set; } = string.Empty;

    /// <summary>
    /// Kierunek sortowania.
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;

    /// <summary>
    /// Czy włączyć Fuzziness (odporność na literówki).
    /// </summary>
    public bool UseFuzziness { get; set; } = true;
}

public enum SortOrder
{
    Ascending,
    Descending
}