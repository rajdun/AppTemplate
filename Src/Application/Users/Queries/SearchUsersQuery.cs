using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Domain.Common;
using FluentResults;
using FluentValidation;

namespace Application.Users.Queries;

public record SearchUsersQuery(PagedUserRequest Request) : IRequest<PagedResult<UserSearchDocumentDto>>;

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

internal class SearchUsersQueryHandler(IUserSearch search)
    : IRequestHandler<SearchUsersQuery, PagedResult<UserSearchDocumentDto>>
{
    public async Task<Result<PagedResult<UserSearchDocumentDto>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken = default)
    {
        var result = await search.SearchUsersAsync(request.Request, cancellationToken).ConfigureAwait(false);
        return Result.Ok(result);
    }
}
