using Application.Common.Dto;

namespace Application.Common.Search.Dto.User;

public class PagedUserRequest : PagedRequest
{
    public StringFilterField? Name { get; set; }
    public StringFilterField? Email { get; set; }
}
