using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal sealed class TracingSubscriptionDomainEventBehavior : ISubscriptionDomainEventBehavior
{
    public async Task HandleAsync(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        var propagationContext = ExtractPropagationContextFromEvent(domainEventWrapper);
        using var activity = TracingHelper.StartConsumerActivity(TracingHelper.EventGridEventsSubscriberActivityName, propagationContext.ActivityContext);

        if (activity == null)
        {
            await next(domainEventWrapper, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await HandleWithTracing(domainEventWrapper, next, activity, cancellationToken).ConfigureAwait(false);
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

    private static async Task HandleWithTracing(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, Activity activity, CancellationToken cancellationToken)
    {
        activity.DisplayName = $"{TracingHelper.EventGridEventsSubscriberActivityName} {domainEventWrapper.DomainEventName}";

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