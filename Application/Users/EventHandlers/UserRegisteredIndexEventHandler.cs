using Application.Common.Elasticsearch;
using Application.Common.Elasticsearch.Models;
using Application.Common.ExtensionMethods;
using Application.Common.Mailing.Templates;
using Domain.Common;
using Domain.DomainEvents.User;
using Domain.Entities.Users;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class UserRegisteredIndexEventHandler(ILogger<UserRegisteredSendEmailEventHandler> logger, IElasticSearchService<ElasticUser> elasticSearchService, UserManager<ApplicationUser>  userManager)
    : IRequestHandler<UserRegistered>
{
    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new())
    {
        var user = await userManager.FindByNameAsync(request.Name);

        if (user == null)
        {
            logger.LogError($"User {request.Email} not found");
            throw new ArgumentNullException("user", "User name not found");
        }

        var result = await elasticSearchService.IndexDocumentAsync(new ElasticUser()
        {
            Email = request.Email,
            Name = request.Name,
            Id = user.Id.ToString()
        });

        if (!result)
        {
            logger.LogError($"Could not index user {request.Email}");
            throw new ArgumentNullException("user", "User name not found");
        }

        return Result.Ok();
    }
}