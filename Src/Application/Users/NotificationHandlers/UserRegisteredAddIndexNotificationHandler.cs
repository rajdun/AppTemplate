using Application.Common.Search;
using Application.Common.Search.Dto.User;
using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common.Interfaces;
using FluentResults;

namespace Application.Users.NotificationHandlers;

public class UserRegisteredAddIndexNotificationHandler(ISearch<UserSearchDocumentDto> search) : IRequestHandler<UserRegistered>
{
    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        await search.IndexAsync([new UserSearchDocumentDto(request.Id, request.Name, request.Email)], cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }
}
