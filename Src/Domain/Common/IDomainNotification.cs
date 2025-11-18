using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Domain.Common;

public interface IDomainNotification : IRequest
{
}

public interface IDomainNotificationDeserializer
{
    dynamic? Deserialize(string eventType, string eventPayload);
}

public class DomainNotificationDeserializer : IDomainNotificationDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Type> _eventTypes;
    private readonly ILogger<DomainNotificationDeserializer> _logger;

    public DomainNotificationDeserializer(IEnumerable<Type> domainEventTypes, ILogger<DomainNotificationDeserializer> logger)
    {
        _logger = logger;
        _eventTypes = domainEventTypes
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainNotification).IsAssignableFrom(t))
            .ToDictionary(t => t.AssemblyQualifiedName!, t => t);
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

    public static IEnumerable<Type> ScanDomainNotificationsTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainNotification).IsAssignableFrom(t));
    }
}