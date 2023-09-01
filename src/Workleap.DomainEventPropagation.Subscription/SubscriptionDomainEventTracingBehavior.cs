using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal sealed class SubscriptionDomainEventTracingBehavior : ISubscriptionDomainEventBehavior
{
    public Task Handle(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        var propagationContext = ExtractPropagationContextFromEvent(domainEventWrapper);
        var activity = TracingHelper.StartConsumerActivity(TracingHelper.EventGridEventsSubscriberActivityName, propagationContext.ActivityContext);
        return activity == null ? next(domainEventWrapper, cancellationToken) : HandleWithTracing(domainEventWrapper, next, activity, cancellationToken);
    }

    private static PropagationContext ExtractPropagationContextFromEvent(DomainEventWrapper domainEventWrapper)
    {
        return domainEventWrapper.Metadata.Count > 0
            ? Propagators.DefaultTextMapPropagator.Extract(default, domainEventWrapper.Metadata, ExtractActivityProperties)
            : default;
    }

    private static IEnumerable<string> ExtractActivityProperties(Dictionary<string, string> activityProperties, string key)
    {
        return activityProperties.TryGetValue(key, out var value) ? new[] { value } : Enumerable.Empty<string>();
    }

    private static async Task HandleWithTracing(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, Activity activity, CancellationToken cancellationToken)
    {
        using (activity)
        {
            activity.DisplayName = domainEventWrapper.DomainEventName;

            try
            {
                await next(domainEventWrapper, cancellationToken).ConfigureAwait(false);

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