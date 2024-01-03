using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;

        this.Services.AddSingleton<IDomainEventTypeRegistry, DomainEventTypeRegistry>();

        this.Services.AddSingleton<EventPuller>();
        this.Services.AddHostedService<EventPuller>(sp => sp.GetRequiredService<EventPuller>());
    }

    public IServiceCollection Services { get; }
}