using Application.Common.MediatorPattern;
using Domain.Common.Interfaces;
using Domain.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public class DomainEventDispatcherInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private const int MaxDispatchRounds = 10;

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

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var dispatchRound = 0;

        while (dispatchRound < MaxDispatchRounds)
        {
            var domainEvents = GetAndClearDomainEvents(context);

            if (domainEvents.Count == 0)
            {
                break;
            }

            foreach (var domainEvent in domainEvents)
            {
                await mediator.PublishAsync((dynamic)domainEvent, cancellationToken).ConfigureAwait(false);
            }

            dispatchRound++;
        }

        if (dispatchRound >= MaxDispatchRounds)
        {
            throw new InvalidOperationException(
                $"Domain event dispatch exceeded {MaxDispatchRounds} rounds. " +
                "This likely indicates an infinite loop where handlers keep raising new events.");
        }
    }

    private static List<IDomainEvent> GetAndClearDomainEvents(DbContext context)
    {
        var entitiesWithDomainEvents = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0)
        {
            return [];
        }

        var domainEvents = entitiesWithDomainEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithDomainEvents.ForEach(e => e.ClearDomainEvents());

        return domainEvents;
    }
}
