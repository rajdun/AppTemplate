using Application.Common;
using Domain.Common.Interfaces;
using FluentResults;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserRegistered;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserRegistered;

namespace Application.Users.EventHandlers;

public class UserRegisteredHandler : IRequestHandler<DomainEvent>
{
    private readonly IApplicationDbContext _context;

    public UserRegisteredHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DomainEvent request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        await _context.AddDomainNotification(new DomainNotification(request.ProfileId, request.Name, request.Email, request.Language)).ConfigureAwait(false);

        return Result.Ok();
    }
}
