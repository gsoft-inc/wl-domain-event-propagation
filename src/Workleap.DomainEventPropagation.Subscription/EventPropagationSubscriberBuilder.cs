using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    private readonly DomainEventTypeRegistry _domainEventTypeRegistry;

    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;

        this._domainEventTypeRegistry = services.Where(x => x.ServiceType == typeof(IDomainEventTypeRegistry))
            .Select(x => x.ImplementationInstance)
            .OfType<DomainEventTypeRegistry>()
            .FirstOrDefault() ?? new DomainEventTypeRegistry();

        services.TryAddSingleton<ISubscriptionEventGridWebhookHandler, SubscriptionEventGridWebhookHandler>();
        services.TryAddSingleton<IDomainEventGridWebhookHandler, DomainEventGridWebhookHandler>();
        services.TryAddSingleton<IEventGridRequestHandler, EventGridRequestHandler>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionDomainEventBehavior, TracingSubscriptionDomainEventBehavior>());

        services.TryAddSingleton<IDomainEventTypeRegistry>(this._domainEventTypeRegistry);
    }

    public IServiceCollection Services { get; }

    public IEventPropagationSubscriberBuilder AddDomainEventHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var concreteHandlerTypes = assembly.GetTypes().Where(IsConcreteDomainEventHandlerType);

        foreach (var concreteHandlerType in concreteHandlerTypes)
        {
            this.AddHandler(concreteHandlerType);
        }

        return this;
    }

    private static bool IsConcreteDomainEventHandlerType(Type type)
    {
        return !type.IsAbstract && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
    }

    public IEventPropagationSubscriberBuilder AddDomainEventHandler<TEvent, THandler>()
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        this.AddHandler(typeof(THandler));
        return this;
    }

    private void AddHandler(Type concreteHandlerType)
    {
        foreach (var handlerInterfaceType in FindGenericDomainEventHandlerInterfaces(concreteHandlerType))
        {
            var domainEventType = handlerInterfaceType.GenericTypeArguments[0];
            this._domainEventTypeRegistry.RegisterDomainEvent(domainEventType);
            this.Services.TryAddTransient(handlerInterfaceType, concreteHandlerType);
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