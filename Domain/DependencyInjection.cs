using Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddDomainEvents();
        return services;
    }

    private static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        var domainEventTypes = DomainNotificationDeserializer.ScanDomainNotificationsTypes(typeof(IDomainNotification).Assembly);
        services.AddSingleton(domainEventTypes);
        services.AddSingleton<IDomainNotificationDeserializer, DomainNotificationDeserializer>();

        return services;
    }
}