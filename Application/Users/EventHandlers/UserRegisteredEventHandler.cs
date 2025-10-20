using Application.Common.Mediator;
using Domain.Common;
using Domain.DomainEvents.User;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class UserRegisteredEventHandler(ILogger <UserRegisteredEventHandler> logger)
    : IRequestHandler<UserRegistered>
{
    public async Task<Result> Handle(UserRegistered request, CancellationToken cancellationToken = new CancellationToken())
    {
        logger.LogInformation("User registered with Name: {UserName}, Email: {UserEmail}", request.Name, request.Email);

        await Task.Delay(150, cancellationToken);
        
        return Result.Ok();
    }
}