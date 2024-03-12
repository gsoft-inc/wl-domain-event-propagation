using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal sealed class TracingDomainEventBehavior : IDomainEventBehavior
{
    public async Task<EventProcessingStatus> HandleAsync(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        var activityName = GetSubscribingActivityName(domainEventWrapper);
        var propagationContext = ExtractPropagationContextFromEvent(domainEventWrapper);

        using var activity = TracingHelper.StartConsumerActivity(activityName, propagationContext.ActivityContext);

        if (activity == null)
        {
            return await next(domainEventWrapper, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await HandleWithTracing(domainEventWrapper, next, activity, cancellationToken).ConfigureAwait(false);
        }
    }

    private static PropagationContext ExtractPropagationContextFromEvent(DomainEventWrapper domainEventWrapper)
    {
        return Propagators.DefaultTextMapPropagator.Extract(default, domainEventWrapper, ExtractActivityProperties);
    }

    private static IEnumerable<string> ExtractActivityProperties(DomainEventWrapper domainEventWrapper, string key)
    {
        return domainEventWrapper.TryGetMetadata(key, out var value) ? new[] { value! } : Enumerable.Empty<string>();
    }

    private static async Task<EventProcessingStatus> HandleWithTracing(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, Activity activity, CancellationToken cancellationToken)
    {
        try
        {
            var result = await next(domainEventWrapper, cancellationToken).ConfigureAwait(false);

            // The message has been processed successfully. Even if the result is Rejected, we consider the processing as a success.
            TracingHelper.MarkAsSuccessful(activity);
            return result;
        }
        catch (Exception ex)
        {
            TracingHelper.MarkAsFailed(activity, ex);
            throw;
        }
    }
    
    private static string GetSubscribingActivityName(DomainEventWrapper domainEventWrappers) => domainEventWrappers.DomainEventSchema switch
    {
        EventSchema.EventGridEvent => TracingHelper.GetEventGridEventsSubscriberActivityName(domainEventWrappers.DomainEventName),
        EventSchema.CloudEvent => TracingHelper.GetCloudEventsSubscriberActivityName(domainEventWrappers.DomainEventName),
        _ => TracingHelper.GetEventGridEventsPublisherActivityName(domainEventWrappers.DomainEventName),
    };
}