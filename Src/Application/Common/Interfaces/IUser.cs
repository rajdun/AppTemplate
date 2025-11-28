using Application.Common.Mailing;
using Application.Common.ValueObjects;

namespace Application.Common.Interfaces;

public interface IUser
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string Email { get; }
    public bool IsAuthenticated { get; }
    public bool IsAdmin { get; }
    public AppLanguage Language { get; }
}
