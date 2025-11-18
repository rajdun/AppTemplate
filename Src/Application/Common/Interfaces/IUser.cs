using Application.Common.Mailing;
using Application.Common.ValueObjects;

namespace Application.Common.Interfaces;

public interface IUser
{
    Guid UserId { get; }
    string UserName { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    AppLanguage Language { get; }
}