using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Domain.Common;
using Domain.DomainNotifications.User;
using Domain.Entities.Users;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public partial class UserRegisteredIndexNotificationHandler(ILogger<UserRegisteredSendEmailNotificationHandler> logger, IElasticSearchService<ElasticUser> elasticSearchService, UserManager<ApplicationUser> userManager)
    : IRequestHandler<UserRegistered>
{
    [LoggerMessage(LogLevel.Error, "User {Email} not found")]
    private static partial void LogUserNotFound(ILogger logger, string email);

    [LoggerMessage(LogLevel.Error, "Could not index user {Email}")]
    private static partial void LogCouldNotIndexUser(ILogger logger, string email);

    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByNameAsync(request.Name).ConfigureAwait(false);

        if (user == null)
        {
            LogUserNotFound(logger, request.Email);
            throw new InvalidOperationException("User name not found");
        }

        var result = await elasticSearchService.IndexDocumentAsync(new ElasticUser()
        {
            Email = request.Email,
            Name = request.Name,
            Id = user.Id.ToString()
        }).ConfigureAwait(false);

        if (!result)
        {
            LogCouldNotIndexUser(logger, request.Email);
            throw new InvalidOperationException("User name not found");
        }

        return Result.Ok();
    }
}
