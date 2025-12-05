namespace Application.Common.Search.Dto;

// A Meilisearch-friendly, engine-agnostic query DTO
public record SearchQuery(
    string? SearchTerm,
    IReadOnlyList<SearchFilter> Filters,
    IReadOnlyList<SearchSort>? Sort,
    int Offset,
    int Limit,
    // Optional per-query settings for Meilisearch and similar engines
    IReadOnlyList<string>? AttributesToSearch = null,
    IReadOnlyList<string>? AttributesToRetrieve = null,
    IReadOnlyList<string>? Facets = null,
    IReadOnlyList<string>? FacetFilters = null
);

public record SearchFilter(
    string Field,
    SearchFilterOperator Operator,
    IReadOnlyList<string>? Values = null,
    string? Value = null,
    LogicalOperator LogicalWithPrevious = LogicalOperator.And
);

public enum SearchFilterOperator
{
    Eq,
    NotEq,
    Gt,
    Gte,
    Lt,
    Lte,
    In,
    NotIn,
    IsNull,
    IsNotNull
}

public enum LogicalOperator
{
    And,
    Or
}

public record SearchSort(string Field, bool Descending);
