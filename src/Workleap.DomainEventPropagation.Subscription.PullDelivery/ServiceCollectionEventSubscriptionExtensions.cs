using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public static class ServiceCollectionEventSubscriptionExtensions
{
    public static IEventPullerBuilder AddPullDeliverySubscription(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return new EventPullerBuilder(services);
    }
}