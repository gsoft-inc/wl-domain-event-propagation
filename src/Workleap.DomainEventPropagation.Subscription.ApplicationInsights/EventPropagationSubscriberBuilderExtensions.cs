using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation;

public static class EventPropagationSubscriberBuilderExtensions
{
    public static IEventPropagationSubscriberBuilder AddApplicationInsights(this IEventPropagationSubscriberBuilder builder)
    {
        builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(ISubscriptionDomainEventBehavior), typeof(SubscriptionApplicationInsightsTracingBehavior), ServiceLifetime.Singleton));

        return builder;
    }
}