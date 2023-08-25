using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal class SubscribtionDomainEventTracingBehavior : ISubscribtionDomainEventBehavior
{
    public Task Handle(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        var context = Propagators.DefaultTextMapPropagator.Extract(
            new PropagationContext(new ActivityContext(), Baggage.Current),
            (domainEvent as IDomainEvent)!.ExtensionAttributes ?? new Dictionary<string, string>(),
            (properties, key) =>
            {
                var valueFromProps = properties.TryGetValue(key, out var propertyValue)
                    ? propertyValue
                    : string.Empty;
                return new List<string> { valueFromProps };
            });

        var activity = TracingHelper.StartActivity(TracingHelper.EventGridEventsSubscriberActivityName, context.ActivityContext);

        return activity == null ? next(domainEvent) : HandleWithTracing(domainEvent, next, activity);
    }

    private static async Task HandleWithTracing(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, Activity activity)
    {
        using (activity)
        {
            try
            {
                await next(domainEvent).ConfigureAwait(false);
                TracingHelper.MarkAsSuccessful(activity);
            }
            catch (Exception ex)
            {
                TracingHelper.MarkAsFailed(activity, ex);

                throw;
            }
        }
    }
}