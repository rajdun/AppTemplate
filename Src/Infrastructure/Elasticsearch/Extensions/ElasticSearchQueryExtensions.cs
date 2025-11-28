using Application.Common.Dto;
using Application.Common.Elasticsearch.Dto;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Infrastructure.Elasticsearch.Extensions;

internal static class ElasticSearchQueryExtensions
{
    internal static List<SortOptions> AddSortOptions(this List<SortOptions> sortOptions, PagedUserRequest request)
    {
        if (string.IsNullOrEmpty(request.SortBy))
        {
            return sortOptions;
        }

        var esSortOrder = request.SortOrder == SortDirection.Asc
            ? Elastic.Clients.Elasticsearch.SortOrder.Asc
            : Elastic.Clients.Elasticsearch.SortOrder.Desc;

        sortOptions.Add(new SortOptions
        {
            Field = new FieldSort()
            {
                Field = new Field(request.GetActualSortField()!),
                Order = esSortOrder
            }
        });

        return sortOptions;
    }

    internal static List<Query> AddQueryFilters(this List<Query> filters, PagedUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return filters;
        }

        if (request.QueryColumns.Count > 0)
        {
            filters.Add(new Query
            {
                MultiMatch = new MultiMatchQuery
                {
                    Query = request.Query,
                    Fields = Fields.FromStrings(request.QueryColumns.ToArray()),
                    Type = TextQueryType.BestFields,
                    Fuzziness = new Fuzziness("AUTO")
                }
            });
        }
        else
        {
            filters.Add(new Query
            {
                QueryString = new QueryStringQuery { Query = request.Query }
            });
        }

        return filters;
    }

    internal static List<Query> AddStringFilter(this List<Query> filters, Field field, StringFilterField? filter)
    {
        if (filter == null)
        {
            return filters;
        }

        if (!string.IsNullOrWhiteSpace(filter.IsEqual))
        {
            filters.Add(new Query
            {
                Term = new TermQuery
                {
                    Value = filter.IsEqual,
                    Field = field.Name + ".keyword"
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(filter.IsNotEqual))
        {
            filters.Add(new Query
            {
                Bool = new BoolQuery
                {
                    MustNot = new List<Query>
                    {
                        new Query
                        {
                            Term = new TermQuery
                            {
                                Value = filter.IsNotEqual,
                                Field = field.Name + ".keyword"
                            }
                        }
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(filter.StartsWith))
        {
            filters.Add(new Query
            {
                MatchPhrasePrefix = new MatchPhrasePrefixQuery
                {
                    Query = filter.StartsWith,
                    Field = field
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(filter.Contains))
        {
            filters.Add(new Query()
            {
                Wildcard = new WildcardQuery()
                {
                    Field = field.Name + ".keyword",
                    Value = $"*{filter.Contains}*",
                    CaseInsensitive = true
                }
            });
        }

        if (filter.InArray != null && filter.InArray.Count > 0)
        {
            filters.Add(new Query
            {
                Terms = new TermsQuery
                {
                    Field = field.Name + ".keyword",
                    Terms = new TermsQueryField(filter.InArray.Select(FieldValue.String).ToList())
                }
            });
        }

        if (filter.NotInArray != null && filter.NotInArray.Count > 0)
        {
            filters.Add(new Query
            {
                Bool = new BoolQuery
                {
                    MustNot = new List<Query>
                    {
                        new Query
                        {
                            Terms = new TermsQuery
                            {
                                Field = field.Name + ".keyword",
                                Terms = new TermsQueryField(filter.NotInArray.Select(FieldValue.String).ToList())
                            }
                        }
                    }
                }
            });
        }

        if (filter.IsNull.HasValue)
        {
            var existsQuery = new Query
            {
                Exists = new ExistsQuery
                {
                    Field = field
                }
            };

            if (filter.IsNull.Value)
            {
                filters.Add(new Query
                {
                    Bool = new BoolQuery
                    {
                        MustNot = new List<Query> { existsQuery }
                    }
                });
            }
            else
            {
                filters.Add(existsQuery);
            }
        }

        return filters;
    }
}
