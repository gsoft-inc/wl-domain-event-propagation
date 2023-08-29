using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation;

public static class EventPropagationSubscriberBuilderExtensions
{
    public static IEventPropagationSubscriberBuilder AddAppInsights(this IEventPropagationSubscriberBuilder builder)
    {
        var tracingBehaviorIdx = -1;

        for (var i = 0; i < builder.Services.Count; i++)
        {
            var implementationType = builder.Services[i].ImplementationType;

            if (implementationType == typeof(SubscriptionDomainEventTracingBehavior) && tracingBehaviorIdx == -1)
            {
                tracingBehaviorIdx = i;
            }
            else if (implementationType == typeof(SubscriptionApplicationInsightsTracingBehavior))
            {
                // ApplicationInsights behaviors already added
                return builder;
            }
        }

        if (tracingBehaviorIdx != -1)
        {
            builder.Services.Insert(tracingBehaviorIdx + 1, new ServiceDescriptor(typeof(ISubscriptionDomainEventBehavior), typeof(SubscriptionApplicationInsightsTracingBehavior), ServiceLifetime.Singleton));
        }

        return builder;
    }
}