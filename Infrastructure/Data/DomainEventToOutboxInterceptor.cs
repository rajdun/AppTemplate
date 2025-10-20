using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data;

public class DomainEventToOutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    
    private void ConvertDomainEventsToOutboxMessages(DbContext? context)
    {
        if (context is null) return;
        
        var entitiesWithDomainEvents = context.ChangeTracker
            .Entries<IEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0) return;
        
        var domainEvents = entitiesWithDomainEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();
        
        entitiesWithDomainEvents.ForEach(e => e.ClearDomainEvents());
        
        var outboxMessages = domainEvents.Select(domainEvent =>
            new OutboxMessage
            {
                EventType = domainEvent.GetType().AssemblyQualifiedName!,
                EventPayload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null,
                RetryCount = 0
            });
        
        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}