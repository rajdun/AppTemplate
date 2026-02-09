using Application.Common;
using Microsoft.EntityFrameworkCore;
using DomainEvent = Domain.Aggregates.Identity.DomainEvents.UserRegistered;
using DomainNotification = Domain.Aggregates.Identity.DomainNotifications.UserRegistered;

namespace Application.Identity.EventHandlers;

public class UserRegisteredDomainEvents
{
    private readonly IApplicationDbContext _context;

    public UserRegisteredDomainEvents(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // Find the aggregate that raised this event
        var userProfile = await _context.GetSet<Domain.Aggregates.Identity.UserProfile>()
            .FirstOrDefaultAsync(u => u.Id == domainEvent.Id, cancellationToken).ConfigureAwait(false);

        if (userProfile is null)
        {
            return;
        }

        userProfile.AddDomainNotification(new DomainNotification(domainEvent.Id, domainEvent.Name, domainEvent.Email,
            domainEvent.Language));
    }
}
