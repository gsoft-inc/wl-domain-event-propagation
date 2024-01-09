using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

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
        this.Services.TryAddSingleton<IDomainEventTypeRegistry>(this._domainEventTypeRegistry);

        this.Services.AddTransient<IEventGridClientWrapperFactory, EventGridClientAdapterFactory>();
        this.Services.AddTransient<ICloudEventHandler, CloudEventHandler>();
        this.Services.AddHostedService<EventPuller>();
    }

    public IServiceCollection Services { get; }

    public IEventPropagationSubscriberBuilder AddDomainEventHandlers(Assembly assembly)
    {
        if (this.Services.All(x => x.ServiceType != typeof(EventGridClientDescriptor)))
        {
            throw new InvalidOperationException("No subscriber was found. Please call AddSubscriber before calling AddDomainEventHandlers.");
        }

        this.Services.AddDomainEventHandlers(this._domainEventTypeRegistry, assembly);
        return this;
    }

    public IEventPropagationSubscriberBuilder AddDomainEventHandler<TEvent, THandler>()
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        if (this.Services.All(x => x.ServiceType != typeof(EventGridClientDescriptor)))
        {
            throw new InvalidOperationException("No subscriber was found. Please call AddSubscriber before calling AddDomainEventHandler.");
        }

        this.Services.AddDomainEventHandler<TEvent, THandler>(this._domainEventTypeRegistry);
        return this;
    }
}