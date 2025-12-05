using Application.Common.Search;
using Application.Common.Search.Dto;
using Domain.Common;
using Domain.DomainNotifications.User;
using Domain.Entities.Users;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public partial class UserRegisteredIndexNotificationHandler(ILogger<UserRegisteredIndexNotificationHandler> logger, ISearch<UserSearchDocumentDto> search, UserManager<ApplicationUser> userManager)
    : IRequestHandler<UserRegistered>
{
    [LoggerMessage(LogLevel.Error, "User {Email} not found")]
    private static partial void LogUserNotFound(ILogger logger, string email);

    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByNameAsync(request.Name).ConfigureAwait(false);

        if (user == null)
        {
            LogUserNotFound(logger, request.Email);
            throw new InvalidOperationException("User name not found");
        }

        await search.IndexAsync(new[]
        {
            new UserSearchDocumentDto(user.Id, request.Name, request.Email)
        }, cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }
}
