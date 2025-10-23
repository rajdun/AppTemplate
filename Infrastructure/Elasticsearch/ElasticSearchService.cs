using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using SearchRequest = Elastic.Clients.Elasticsearch.SearchRequest;
using SortOrder = Elastic.Clients.Elasticsearch.SortOrder;

namespace Infrastructure.Elasticsearch;

internal class ElasticSearchService<T> : IElasticSearchService<T> where T : class, IElasticDocument
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchService<T>> _logger;
    private readonly string _indexName;

    public ElasticSearchService(
        ElasticsearchClient client,
        ILogger<ElasticSearchService<T>> logger)
    {
        _client = client;
        _logger = logger;

        _indexName = typeof(T).Name.ToLowerInvariant();
    }


    public async Task<bool> IndexDocumentAsync(T document)
    {
        var response = await _client.IndexAsync(document, req => req
            .Index(_indexName)
            .Id(document.Id)
        );

        if (!response.IsSuccess())
        {
            _logger.LogError("Błąd podczas indeksowania dokumentu {Id} w indeksie {Index}: {Error}", document.Id,
                _indexName, response.DebugInformation);
            return false;
        }

        return true;
    }

    public async Task<T?> GetDocumentAsync(string id)
    {
        var response = await _client.GetAsync<T>(id, cfg => cfg.Index(_indexName));
        if (!response.IsSuccess())
        {
            _logger.LogWarning("Nie udało się pobrać dokumentu {Id} z indeksu {Index}: {Error}", id, _indexName,
                response.DebugInformation);
            return null;
        }

        return response.Source;
    }

    public async Task<bool> DeleteDocumentAsync(string id)
    {
        var response = await _client.DeleteAsync(_indexName, id);
        if (!response.IsSuccess())
        {
            _logger.LogError("Błąd podczas usuwania dokumentu {Id} z indeksu {Index}: {Error}", id, _indexName,
                response.DebugInformation);
            return false;
        }

        return true;
    }

    public async Task<SearchResult<T>> SearchAsync(Application.Common.Elasticsearch.Dto.SearchRequest request)
    {
        // Wymuszenie rozsądnych limitów, nawet jeśli walidacja DTO zawiedzie
        if (request.PageSize > 1000) request.PageSize = 1000;
        if (request.PageSize < 1) request.PageSize = 1;
        if (request.PageNumber < 1) request.PageNumber = 1;

        // Obliczenie paginacji dla Elasticsearch (jest 0-based)
        int from = (request.PageNumber - 1) * request.PageSize;

        // --- 2. Wykonanie zapytania do klienta Elastic ---
        var response = await _client.SearchAsync<T>(s => s
                .Indices(_indexName)
                .From(from)
                .Size(request.PageSize)
                .Query(q => BuildQuery(q, request)) // Delegowanie budowania zapytania
                .Sort(so => BuildSort(so, request)) // Delegowanie budowania sortowania
        );

        // --- 3. Obsługa odpowiedzi ---
        if (!response.IsSuccess())
        {
            _logger.LogError("Błąd podczas wyszukiwania w indeksie {Index}: {Error}",
                _indexName, response.DebugInformation);

            // Zwróć pusty obiekt, ale z informacjami o paginacji z żądania
            return new SearchResult<T>
            {
                Page = request.PageNumber,
                PageSize = request.PageSize
                // TotalCount i Documents pozostają domyślne (0 i pusta lista)
            };
        }

        // --- 4. Mapowanie pomyślnego wyniku ---
        return new SearchResult<T>
        {
            Documents = response.Documents,
            TotalCount = response.Total,
            Page = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Metoda pomocnicza do budowania dynamicznego zapytania (Query)
    /// </summary>
    private void BuildQuery(QueryDescriptor<T> q, Application.Common.Elasticsearch.Dto.SearchRequest request)
    {
        // Przypadek 1: Brak tekstu wyszukiwania -> zwróć wszystko (spaginowane)
        if (string.IsNullOrWhiteSpace(request.QueryText))
        {
            q.MatchAll();
            return;
        }

        // Przypadek 2: Tekst istnieje, ale brak pól -> zablokuj (ze względów wydajności)
        if (request.FieldsToSearch == null || !request.FieldsToSearch.Any())
        {
            _logger.LogWarning("Wyszukiwanie z tekstem '{Query}' nie ma zdefiniowanych pól. Blokowanie zapytania.",
                request.QueryText);
            // MatchNone() to zapytanie, które celowo nic nie zwraca
            q.MatchNone();
            return;
        }

        // Przypadek 3: Standardowe zapytanie MultiMatch
        q.MultiMatch(mm => mm
                .Query(request.QueryText)
                .Fields(request.FieldsToSearch.ToArray())
                .Fuzziness(request.UseFuzziness ? new Fuzziness("AUTO") : new Fuzziness("0"))
                .Operator(Operator.And) // Wymaga wszystkich słów w zapytaniu (bardziej precyzyjne)
        );
    }

    /// <summary>
    /// Metoda pomocnicza do budowania dynamicznego sortowania (Sort)
    /// </summary>
    private void BuildSort(SortOptionsDescriptor<T> so, Application.Common.Elasticsearch.Dto.SearchRequest request)
    {
        // Przypadek 1: Brak zdefiniowanego pola -> sortuj po trafności (_score)
        if (string.IsNullOrWhiteSpace(request.SortField))
        {
            so.Score(s => s.Order(Elastic.Clients.Elasticsearch.SortOrder.Desc));
            return;
        }

        // Przypadek 2: Mamy pole sortowania -> przekonwertuj kierunek
        var order = request.SortOrder == Application.Common.Elasticsearch.Dto.SortOrder.Ascending
            ? Elastic.Clients.Elasticsearch.SortOrder.Asc
            : Elastic.Clients.Elasticsearch.SortOrder.Desc;

        // --- WAŻNE (Produkcja) ---
        // Aby poprawnie sortować pola tekstowe (np. "name"), musimy użyć ich sub-pola ".keyword".
        // Jednocześnie chcemy wspierać sortowanie pól liczbowych/dat (np. "price").
        // Ta strategia obsługuje oba przypadki:

        // 1. Spróbuj sortować po ".keyword" (dla pól tekstowych)
        so.Field($"{request.SortField}.keyword", f => f
                .Order(order)
                .UnmappedType(FieldType.Keyword) // Ignoruj, jeśli pole nie ma sub-pola .keyword
        );

        // 2. Spróbuj sortować po polu głównym (dla pól liczbowych, dat)
        so.Field(request.SortField, f => f
            .Order(order)
        );
    }
}