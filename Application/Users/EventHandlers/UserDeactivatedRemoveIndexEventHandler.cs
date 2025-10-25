using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Domain.Common;
using Domain.DomainEvents.User;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class UserDeactivatedRemoveIndexEventHandler(IElasticSearchService<ElasticUser> elasticSearchService, ILogger<UserDeactivatedRemoveIndexEventHandler> logger)
    : IRequestHandler<UserDeactivated>
{
    public async Task<Result> Handle(UserDeactivated request, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await elasticSearchService.DeleteDocumentAsync(request.UserId.ToString());
        
        if (!result)
        {
                        logger.LogError("Could not remove user {UserId} from index", request.UserId);
            return Result.Fail("Could not remove user from index");
        }

        return Result.Ok();
    }
}