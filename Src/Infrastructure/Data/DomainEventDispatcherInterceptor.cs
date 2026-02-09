using Application.Identity.EventHandlers;
using Domain.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcherInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(eventData);
        await DispatchDomainEvents(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchDomainEvents(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            return;
        }

        var entitiesWithDomainEvents = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0)
        {
            return;
        }

        var domainEvents = entitiesWithDomainEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before dispatching to prevent re-processing
        entitiesWithDomainEvents.ForEach(e => e.ClearDomainEvents());

        using (var scope = _serviceProvider.CreateScope())
        {
            // Dispatch events - handlers may add notifications to the aggregates
            foreach (var domainEvent in domainEvents)
            {
                switch (domainEvent)
                {
                    case Domain.Aggregates.Identity.DomainEvents.UserRegistered userRegistered:
                    {
                        var handler = scope.ServiceProvider.GetService<UserRegisteredDomainEvents>();
                        if (handler != null)
                        {
                            await handler.Handle(userRegistered, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                    case Domain.Aggregates.Identity.DomainEvents.UserDeactivated userDeactivated:
                    {
                        var handler = scope.ServiceProvider.GetService<UserDeactivatedDomainEvents>();
                        if (handler != null)
                        {
                            await handler.Handle(userDeactivated, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                }
            }
        }
    }
}
