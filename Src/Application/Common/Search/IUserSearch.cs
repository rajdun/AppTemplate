using Application.Common.Dto;
using Application.Common.Search.Dto;

namespace Application.Common.Search;

public interface IUserSearch
{
    public Task<PagedResult<UserSearchDocumentDto>> SearchUsersAsync(PagedUserRequest request, CancellationToken cancellationToken = default);
}
