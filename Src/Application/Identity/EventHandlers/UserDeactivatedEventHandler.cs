using Application.Common;
using Microsoft.EntityFrameworkCore;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserDeactivated;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserDeactivated;

namespace Application.Identity.EventHandlers;

public class UserDeactivatedDomainEvents
{
    private readonly IApplicationDbContext _context;

    public UserDeactivatedDomainEvents(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // Find the aggregate that raised this event
        var userProfile = await _context.GetSet<Domain.Aggregates.Identity.UserProfile>()
            .FirstOrDefaultAsync(u => u.Id == domainEvent.UserId, cancellationToken).ConfigureAwait(false);

        if (userProfile is null)
        {
            return;
        }

        userProfile.AddDomainNotification(new DomainNotification(domainEvent.UserId));
    }
}
