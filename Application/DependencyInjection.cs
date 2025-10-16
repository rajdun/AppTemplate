using System.Reflection;
using Application.Common.Mediator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
    
    private static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IMediator, Mediator>();

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var type in handlerTypes)
        {
            var interfaceType = type.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
            
            services.AddScoped(interfaceType, type);
        }

        return services;
    }
}