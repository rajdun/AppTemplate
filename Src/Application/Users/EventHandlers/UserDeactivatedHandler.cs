using Application.Common;
using Domain.Common.Interfaces;
using FluentResults;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserDeactivated;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserDeactivated;

namespace Application.Users.EventHandlers;

public class UserDeactivatedHandler : IRequestHandler<DomainEvent>
{
    private readonly IApplicationDbContext _context;

    public UserDeactivatedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DomainEvent request, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(request);

        await _context.AddDomainNotification(new DomainNotification(request.ProfileId)).ConfigureAwait(false);

        return Result.Ok();
    }
}
