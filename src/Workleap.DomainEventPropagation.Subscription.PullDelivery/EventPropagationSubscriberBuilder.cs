using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;

        this.Services.AddSingleton<IDomainEventTypeRegistry, DomainEventTypeRegistry>();
        this.Services.AddHostedService<EventPuller>();
    }

    public IServiceCollection Services { get; }
}