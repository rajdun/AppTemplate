using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Domain.Common;

#pragma warning disable CA1040
public interface IDomainNotification : IRequest
#pragma warning restore CA1040
{
}

public interface IDomainNotificationDeserializer
{
    public dynamic? Deserialize(string eventType, string eventPayload);
}

public partial class DomainNotificationDeserializer : IDomainNotificationDeserializer
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
            LogEventTypeNotFound(eventType);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(eventPayload, type, JsonOptions);
        }
        catch (JsonException ex)
        {
            LogJsonError(ex, eventType);
            return null;
        }
    }

    public static IEnumerable<Type> ScanDomainNotificationsTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainNotification).IsAssignableFrom(t));
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "JSON deserialization error for event type {EventType}")]
    private partial void LogJsonError(Exception ex, string eventType);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Event type '{EventType}' not found.")]
    private partial void LogEventTypeNotFound(string eventType);
}
