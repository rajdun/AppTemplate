using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public class UserDeactivatedRemoveIndexNotificationHandler(ISearch<UserSearchDocumentDto> search)
    : IRequestHandler<UserDeactivated>
{
    public async Task<Result> Handle(UserDeactivated request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        await search.DeleteAsync(new[] { request.UserId }, cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }
}
