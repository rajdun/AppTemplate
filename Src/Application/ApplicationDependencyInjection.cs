using System.Reflection;
using Application.Common.MediatorPattern;
using Application.Common.Messaging;
using Domain.Common;
using Domain.Common.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(typeof(ApplicationDependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjection).Assembly);


        return services;
    }

    private static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IMediator, Mediator>();

        var handlerTypesWithTypeResults = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var type in handlerTypesWithTypeResults)
        {
            var interfaceType = type.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            services.AddScoped(interfaceType, type);
        }


        var handlerTypesWithGenericResults = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
            .ToList();

        foreach (var type in handlerTypesWithGenericResults)
        {
            var interfaceType = type.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<>));

            services.AddScoped(interfaceType, type);
        }

        return services;
    }
}
