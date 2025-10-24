
using Application.Common.Elasticsearch.Dto;

namespace Application.Common.Elasticsearch;

public interface IElasticSearchService<T> where T : class, IElasticDocument
{
    /// <summary>
    /// Indeksuje (dodaje lub aktualizuje) dokument.
    /// </summary>
    Task<bool> IndexDocumentAsync(T document);

    /// <summary>
    /// Pobiera dokument po jego identyfikatorze.
    /// </summary>
    Task<T?> GetDocumentAsync(string id);

    /// <summary>
    /// Usuwa dokument po jego identyfikatorze.
    /// </summary>
    Task<bool> DeleteDocumentAsync(string id);
}