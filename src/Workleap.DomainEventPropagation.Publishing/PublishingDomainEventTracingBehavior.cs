using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal sealed class PublishingDomainEventTracingBehavior : IPublishingDomainEventBehavior
{
    public Task Handle(IEnumerable<IDomainEvent> events, DomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        var activity = TracingHelper.StartActivity(TracingHelper.EventGridEventsPublisherActivityName);
        return activity == null ? next(events) : HandleWithTracing(events, next, activity);
    }

    private static async Task HandleWithTracing(IEnumerable<IDomainEvent> events, DomainEventsHandlerDelegate next, Activity activity)
    {
        using (activity)
        {
            var serializedTelemetryData = new Dictionary<string, string>();

            try
            {
                var currentActivityContext = Activity.Current is { } currentActivity ? currentActivity.Context : default;
                Propagators.DefaultTextMapPropagator.Inject(
                    new PropagationContext(currentActivityContext, Baggage.Current),
                    serializedTelemetryData,
                    (dict, key, value) =>
                    {
                        dict[key] = value;
                    });

                foreach (var evt in events)
                {
                    evt.ExtensionAttributes = serializedTelemetryData;
                }

                await next(events).ConfigureAwait(false);
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