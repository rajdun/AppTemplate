using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Domain.Common;
using Domain.DomainNotifications.User;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.NotificationHandlers;

public partial class UserDeactivatedRemoveIndexNotificationHandler(IElasticSearchService<ElasticUser> elasticSearchService, ILogger<UserDeactivatedRemoveIndexNotificationHandler> logger)
    : IRequestHandler<UserDeactivated>
{
    [LoggerMessage(0, LogLevel.Error, "Could not remove user {UserId} from index")]
    private static partial void LogCouldNotRemoveUserFromIndex(ILogger logger, Guid userId);

    public async Task<Result> Handle(UserDeactivated request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await elasticSearchService.DeleteDocumentAsync(request.UserId.ToString()).ConfigureAwait(false);

        if (!result)
        {
            LogCouldNotRemoveUserFromIndex(logger, request.UserId);
            return Result.Fail("Could not remove user from index");
        }

        return Result.Ok();
    }
}
