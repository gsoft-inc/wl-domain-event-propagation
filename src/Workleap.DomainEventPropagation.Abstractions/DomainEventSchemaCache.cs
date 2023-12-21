using System.Collections.Concurrent;
using System.Reflection;

namespace Workleap.DomainEventPropagation;

internal static class DomainEventSchemaCache
{
    // This class lives in the abstractions assembly in order to prevent having multiple instances of this same cache in different assemblies at runtime
    private static readonly ConcurrentDictionary<Type, EventSchema> DomainEventSchemaTypeMappings = new ConcurrentDictionary<Type, EventSchema>();

    public static EventSchema GetEventSchema<T>()
        where T : IDomainEvent
    {
        return GetEventSchema(typeof(T));
    }

    public static EventSchema GetEventSchema(Type domainEventType)
    {
        if (!IsConcreteDomainEvent(domainEventType))
        {
            throw new ArgumentException($"{domainEventType} must be a concrete type that implements {nameof(IDomainEvent)}");
        }

        return DomainEventSchemaTypeMappings.GetOrAdd(domainEventType, static type =>
        {
            if (type.GetCustomAttribute<DomainEventAttribute>() is { } attribute)
            {
                return attribute.Schema;
            }

            throw new ArgumentException($"{type} must be decorated with {nameof(DomainEventAttribute)}");
        });
    }

    public static bool IsConcreteDomainEvent(Type type) => !type.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(type);
}