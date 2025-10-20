using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Domain.Common;

public interface IDomainEvent : IRequest
{
    
}

public interface IDomainEventDeserializer
{
    dynamic? Deserialize(string eventType, string eventPayload);
}

public class DomainEventDeserializer : IDomainEventDeserializer
{
    private readonly Dictionary<string, Type> _eventTypes;
    private readonly ILogger<DomainEventDeserializer> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DomainEventDeserializer(IEnumerable<Type> domainEventTypes, ILogger<DomainEventDeserializer> logger)
    {
        _logger = logger;
        _eventTypes = domainEventTypes
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToDictionary(t => t.AssemblyQualifiedName!, t => t);
    }

    public static IEnumerable<Type> ScanDomainEventTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t));
    }

    public dynamic? Deserialize(string eventType, string eventPayload)
    {
        if (!_eventTypes.TryGetValue(eventType, out var type))
        {
            Console.WriteLine($"Error: Event type '{eventType}' not found.");
            return null; 
        }

        try
        {
            return JsonSerializer.Deserialize(eventPayload, type, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for event type {EventType}", eventType);
            return null;
        }
    }
}