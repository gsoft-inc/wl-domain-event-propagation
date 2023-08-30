using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal class SubscriptionDomainEventTracingBehavior : ISubscriptionDomainEventBehavior
{
    public Task Handle(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (domainEvent.GetType().FullName != typeof(DomainEventWrapper).FullName)
        {
            return next(domainEvent);
        }

        var eventWrapper = domainEvent as DomainEventWrapper;

        var context = Propagators.DefaultTextMapPropagator.Extract(
            new PropagationContext(new ActivityContext(), Baggage.Current),
            eventWrapper!.ExtensionAttributes,
            (properties, key) =>
            {
                var valueFromProps = properties.TryGetValue(key, out var propertyValue)
                    ? propertyValue
                    : string.Empty;
                return new List<string> { valueFromProps };
            });

        var actualDomainEvent = (IDomainEvent?)JsonSerializer.Deserialize(eventWrapper.DomainEventJson.ToString(), Type.GetType(eventWrapper.DomainEventType)!);
        if (actualDomainEvent == null)
        {
            throw new InvalidOperationException($"Can't deserialize domainEvent with EventType: {eventWrapper.DomainEventType}.");
        }

        var activity = TracingHelper.StartActivity(TracingHelper.EventGridEventsSubscriberActivityName, context.ActivityContext);

        return activity == null ? next(actualDomainEvent) : HandleWithTracing(actualDomainEvent, next, activity);
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