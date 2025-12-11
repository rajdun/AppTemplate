using System.Reflection;

namespace Domain.Common.Interfaces;

#pragma warning disable CA1040
public interface IDomainEvent : IRequest
#pragma warning restore CA1040
{

}

internal static class DomainEventExtensions
{
    internal static IEnumerable<Type> ScanDomainNotificationsTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t));
    }
}
