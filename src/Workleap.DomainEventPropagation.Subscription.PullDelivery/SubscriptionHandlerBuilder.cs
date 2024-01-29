using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

public interface ISubscriptionHandlerBuilder
{
    IServiceCollection Services { get; }

    ISubscriptionHandlerBuilder AddDomainEventHandler<TEvent, THandler>()
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent;
}

internal sealed class SubscriptionHandlerBuilder : ISubscriptionHandlerBuilder
{
    private readonly IKeyedDomainEventTypeRegistry _keyedDomainEventTypeRegistry;
    private readonly string _key;

    public SubscriptionHandlerBuilder(IServiceCollection services, string key)
    {
        this.Services = services;
        this._key = key;

        this._keyedDomainEventTypeRegistry = services.Where(x => x.ServiceType == typeof(IKeyedDomainEventTypeRegistry) && x.ServiceKey == key)
            .Select(x => x.ImplementationInstance)
            .OfType<DomainEventTypeRegistry>()
            .FirstOrDefault() ?? new DomainEventTypeRegistry();

        this.Services.AddKeyedSingleton(key, this._keyedDomainEventTypeRegistry);
        this.Services.AddKeyedTransient<ISubscriptionHandler>(
            key,
            (sp, _) =>
                new SubscriptionHandler(
                    sp,
                    key,
                    sp.GetRequiredService<IGlobalDomainEventTypeRegistry>(),
                    sp.GetRequiredKeyedService<IKeyedDomainEventTypeRegistry>(key),
                    sp.GetRequiredService<IEnumerable<IDomainEventBehavior>>(),
                    sp.GetRequiredService<ILogger<ISubscriptionHandler>>()));
    }

    public IServiceCollection Services { get; }

    public ISubscriptionHandlerBuilder AddDomainEventHandler<TEvent, THandler>()
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        this.AddHandler(typeof(THandler));
        return this;
    }

    private static bool IsConcreteDomainEventHandlerType(Type type)
    {
        return !type.IsAbstract && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
    }

    private void AddHandler(Type concreteHandlerType)
    {
        foreach (var handlerInterfaceType in FindGenericDomainEventHandlerInterfaces(concreteHandlerType))
        {
            var domainEventType = handlerInterfaceType.GenericTypeArguments[0];
            this._keyedDomainEventTypeRegistry.RegisterDomainEvent(domainEventType);
            this.Services.TryAddKeyedTransient(handlerInterfaceType, this._key, concreteHandlerType);
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