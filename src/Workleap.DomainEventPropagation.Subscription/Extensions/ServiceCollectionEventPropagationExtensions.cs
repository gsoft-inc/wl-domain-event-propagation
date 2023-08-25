using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Workleap.DomainEventPropagation.Events;

namespace Workleap.DomainEventPropagation.Extensions;

public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ISubscriptionEventGridWebhookHandler, SubscriptionEventGridWebhookHandler>();
        services.AddSingleton<IDomainEventGridWebhookHandler, DomainEventGridWebhookHandler>();
        services.AddSingleton<IEventGridRequestHandler, EventGridRequestHandler>();

        services.TryAddEnumerable(new ServiceDescriptor(typeof(ISubscribtionDomainEventBehavior), typeof(SubscribtionDomainEventTracingBehavior), ServiceLifetime.Singleton));

        return new EventPropagationSubscriberBuilder(services);
    }
}

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;
    }

    public IServiceCollection Services { get; }
}