using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Domain.Aggregates.Identity;
using Domain.Aggregates.Identity.DomainNotifications;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public partial class UserRegisteredIndexNotificationHandler(ILogger<UserRegisteredIndexNotificationHandler> logger, ISearch<UserSearchDocumentDto> search, UserManager<User> userManager)
    : IRequestHandler<UserRegistered>
{
    [LoggerMessage(LogLevel.Error, "[UserRegisteredIndex] User {Email} not found in identity store")]
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
