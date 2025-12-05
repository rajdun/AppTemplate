using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Domain.Common;
using FluentResults;
using FluentValidation;

namespace Application.Users.Queries;

public record SearchUsersQuery(PagedRequest Request) : IRequest<PagedResult<UserSearchDocumentDto>>;

public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    private static readonly string[] SortableFields = ["Id", "Name", "Email"];

    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Request.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.Request.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Request.SortBy)
            .Must(sortBy =>
                sortBy == null || SortableFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortableFields)}");
    }
}

internal class SearchUsersQueryHandler(ISearch<UserSearchDocumentDto> search)
    : IRequestHandler<SearchUsersQuery, PagedResult<UserSearchDocumentDto>>
{
    public async Task<Result<PagedResult<UserSearchDocumentDto>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken = new CancellationToken())
    {
        return await search.SearchAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
