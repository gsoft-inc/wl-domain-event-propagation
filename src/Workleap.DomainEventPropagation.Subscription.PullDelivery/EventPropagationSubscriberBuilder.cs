using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.ClientWrapper;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;

        this.Services.AddSingleton<IDomainEventTypeRegistry, DomainEventTypeRegistry>();
        this.Services.AddTransient<IEventGridClientWrapperFactory, EventGridClientWrapperFactory>();
        this.Services.AddTransient<ICloudEventHandler, CloudEventHandler>();
        this.Services.AddHostedService<EventPuller>();
    }

    public IServiceCollection Services { get; }
}