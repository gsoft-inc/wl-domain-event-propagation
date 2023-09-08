﻿using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation;

internal sealed class TracingSubscriptionDomainEventBehavior : ISubscriptionDomainEventBehavior
{
    public Task HandleAsync(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        var propagationContext = ExtractPropagationContextFromEvent(domainEventWrapper);
        using var activity = TracingHelper.StartConsumerActivity(TracingHelper.EventGridEventsSubscriberActivityName, propagationContext.ActivityContext);
        return activity == null ? next(domainEventWrapper, cancellationToken) : HandleWithTracing(domainEventWrapper, next, activity, cancellationToken);
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