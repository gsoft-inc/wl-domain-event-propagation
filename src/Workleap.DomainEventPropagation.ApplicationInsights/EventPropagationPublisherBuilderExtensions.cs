using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation;

public static class EventPropagationPublisherBuilderExtensions
{
    public static IEventPropagationPublisherBuilder AddAppInsights(this IEventPropagationPublisherBuilder builder)
    {
        var tracingBehaviorIdx = -1;

        for (var i = 0; i < builder.Services.Count; i++)
        {
            var implementationType = builder.Services[i].ImplementationType;

            if (implementationType == typeof(PublishingDomainEventTracingBehavior) && tracingBehaviorIdx == -1)
            {
                tracingBehaviorIdx = i;
            }
            else if (implementationType == typeof(PublishigApplicationInsightsTracingBehavior))
            {
                // ApplicationInsights behaviors already added
                return builder;
            }
        }

        if (tracingBehaviorIdx != -1)
        {
            builder.Services.Insert(tracingBehaviorIdx + 1, new ServiceDescriptor(typeof(IPublishingDomainEventBehavior), typeof(PublishigApplicationInsightsTracingBehavior), ServiceLifetime.Singleton));
        }

        return builder;
    }
}