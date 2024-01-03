using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public static class ServiceCollectionEventSubscriptionExtensions
{
    public static IEventPropagationSubscriberBuilder AddPullDeliverySubscription(this IServiceCollection services)
    {
        return new EventPropagationSubscriberBuilder(services);
    }

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder)
        => builder.AddSubscriber(_ => { });

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder, string optionsSectionName)
        => builder.AddSubscriber(_ => { }, optionsSectionName);

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder, Action<EventPropagationSubscriptionOptions> configure, string optionsSectionName = EventPropagationSubscriptionOptions.DefaultSectionName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.ConfigureSubscriber(configure, optionsSectionName);
        return builder;
    }
}