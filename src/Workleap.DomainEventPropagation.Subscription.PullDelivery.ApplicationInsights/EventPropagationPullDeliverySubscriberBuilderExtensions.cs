using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Workleap.DomainEventPropagation;

public static class EventPropagationPullDeliverySubscriberBuilderExtensions
{
    public static IEventPropagationSubscriberBuilder AddApplicationInsights(this IEventPropagationSubscriberBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDomainEventBehavior, ApplicationInsightsSubscriptionDomainEventBehavior>());
        return builder;
    }
}