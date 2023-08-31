using System.Collections.Concurrent;
using System.Reflection;

namespace Workleap.DomainEventPropagation;

internal static class DomainEventNameCache
{
    private static readonly ConcurrentDictionary<Type, string> EventNameTypeMappings = new ConcurrentDictionary<Type, string>();

    public static string GetName(Type domainEventType)
    {
        if (!IsConcreteDomainEventType(domainEventType))
        {
            throw new ArgumentException(domainEventType + " must be a concrete type that implements " + nameof(IDomainEvent));
        }

        return EventNameTypeMappings.GetOrAdd(domainEventType, static domainEventType =>
        {
            if (domainEventType.GetCustomAttribute<DomainEventAttribute>() is { } attribute)
            {
                return attribute.Name;
            }

            throw new ArgumentException(domainEventType + " must be decorated with " + nameof(DomainEventAttribute));
        });
    }

    public static bool IsConcreteDomainEventType(Type type) => !type.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(type);

    public static string GetName<T>()
        where T : IDomainEvent
    {
        return GetName(typeof(T));
    }
}