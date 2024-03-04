using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Workleap.DomainEventPropagation;

internal static class ServiceCollectionExtensions
{
    public static void AddDomainEventHandlers(this IServiceCollection services, IDomainEventTypeRegistry domainEventTypeRegistry, IEnumerable<Type> handlers)
    {
        foreach (var concreteHandlerType in handlers)
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