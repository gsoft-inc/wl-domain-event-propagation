using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation;

public static class EventPropagationPublisherBuilderExtensions
{
    public static IEventPropagationPublisherBuilder AddAppInsights(this IEventPropagationPublisherBuilder builder)
    {
        builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(IPublishingDomainEventBehavior), typeof(PublishigApplicationInsightsTracingBehavior), ServiceLifetime.Singleton));

        return builder;
    }
}