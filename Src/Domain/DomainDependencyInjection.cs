using Domain.Common;
using Domain.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using DomainNotificationDeserializer = Domain.Common.Interfaces.DomainNotificationDeserializer;

namespace Domain;

public static class DomainDependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddDomainEvents();
        services.AddDomainNotifications();
        return services;
    }

    private static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        var domainEventTypes = DomainEventExtensions.ScanDomainNotificationsTypes(typeof(IDomainNotification).Assembly);
        services.AddSingleton(domainEventTypes);

        return services;
    }

    private static IServiceCollection AddDomainNotifications(this IServiceCollection services)
    {
        var domainEventTypes = DomainNotificationDeserializer.ScanDomainNotificationsTypes(typeof(IDomainNotification).Assembly);
        services.AddSingleton(domainEventTypes);
        services.AddSingleton<IDomainNotificationDeserializer, DomainNotificationDeserializer>();

        return services;
    }
}
