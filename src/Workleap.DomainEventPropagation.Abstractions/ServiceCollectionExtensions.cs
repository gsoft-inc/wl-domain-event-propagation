using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Workleap.DomainEventPropagation;

internal static class ServiceCollectionExtensions
{
    public static void AddDomainEventHandlers(this IServiceCollection services, IDomainEventTypeRegistry domainEventTypeRegistry, Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var concreteHandlerTypes = assembly.GetTypes().Where(IsConcreteDomainEventHandlerType);

        foreach (var concreteHandlerType in concreteHandlerTypes)
        {
            AddHandler(services, domainEventTypeRegistry, concreteHandlerType);
        }
    }

    public static void AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services, IDomainEventTypeRegistry domainEventTypeRegistry)
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        AddHandler(services, domainEventTypeRegistry, typeof(THandler));
    }

    private static bool IsConcreteDomainEventHandlerType(Type type)
    {
        return !type.IsAbstract && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
    }

    private static void AddHandler(IServiceCollection services, IDomainEventTypeRegistry domainEventTypeRegistry, Type concreteHandlerType)
    {
        foreach (var handlerInterfaceType in FindGenericDomainEventHandlerInterfaces(concreteHandlerType))
        {
            var domainEventType = handlerInterfaceType.GenericTypeArguments[0];
            domainEventTypeRegistry.RegisterDomainEvent(domainEventType);
            services.TryAddTransient(handlerInterfaceType, concreteHandlerType);
        }
    }

    private static IEnumerable<Type> FindGenericDomainEventHandlerInterfaces(Type handlerType)
    {
        return handlerType.FindInterfaces(FilterGenericInterfaceOf, typeof(IDomainEventHandler<>));

        static bool FilterGenericInterfaceOf(Type interfaceType, object? criteria)
        {
            return interfaceType.IsGenericType && ReferenceEquals(interfaceType.GetGenericTypeDefinition(), criteria);
        }
    }
}