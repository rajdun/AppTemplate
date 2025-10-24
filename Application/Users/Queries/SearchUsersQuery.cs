using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;
using Domain.Common;
using FluentResults;
using FluentValidation;

namespace Application.Users.Queries;

public record SearchUsersQuery(PagedUserRequest Request) : IRequest<PagedResult<ElasticUser>>;

public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Request.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.Request.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Request.SortBy)
            .Must(sortBy =>
                sortBy == null || ElasticUser.SortableFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase));
    }
}

internal class SearchUsersQueryHandler(IUserSearchService userSearchService) 
    : IRequestHandler<SearchUsersQuery, PagedResult<ElasticUser>>
{
    public async Task<Result<PagedResult<ElasticUser>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken = new CancellationToken())
    {
        return await userSearchService.SearchUsersAsync(request.Request, cancellationToken);
    }
}