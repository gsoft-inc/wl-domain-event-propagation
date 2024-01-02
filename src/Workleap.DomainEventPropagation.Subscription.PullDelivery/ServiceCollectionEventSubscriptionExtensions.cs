using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public static class ServiceCollectionEventSubscriptionExtensions
{
    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services)
        => services.AddEventPropagationSubscriber(_ => { });

    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services, Action<EventPropagationSubscriptionOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return new EventPropagationSubscriberBuilder(services, configure);
    }
}