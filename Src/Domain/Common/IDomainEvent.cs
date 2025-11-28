using System.Reflection;

namespace Domain.Common;

public interface IDomainEvent : IRequest
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
