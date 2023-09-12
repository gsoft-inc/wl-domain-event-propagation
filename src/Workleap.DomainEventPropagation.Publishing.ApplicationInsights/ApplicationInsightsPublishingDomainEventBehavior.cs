﻿using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace Workleap.DomainEventPropagation;

internal sealed class ApplicationInsightsPublishingDomainEventBehavior : IPublishingDomainEventBehavior
{
    private readonly TelemetryClient? _telemetryClient;

    public ApplicationInsightsPublishingDomainEventBehavior(TelemetryClient? telemetryClient)
    {
        this._telemetryClient = telemetryClient;
    }

    public Task HandleAsync(DomainEventWrapperCollection domainEventWrappers, DomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null
            ? next(domainEventWrappers, cancellationToken)
            : this.HandleWithTelemetry(domainEventWrappers, next, cancellationToken);
    }

    private async Task HandleWithTelemetry(DomainEventWrapperCollection domainEventWrappers, DomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        var activityName = TracingHelper.GetEventGridEventsPublisherActivityName(domainEventWrappers.DomainEventName);
        var operation = this._telemetryClient!.StartActivityAwareDependencyOperation(activityName);

        foreach (var domainEventWrapper in domainEventWrappers)
        {
            domainEventWrapper.SetMetadata(ApplicationInsightsConstants.ParentOperationIdField, operation.Telemetry.Context.Operation.Id);
        }

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

        try
        {
            operation.Telemetry.Name = domainEventWrappers.DomainEventName;
            operation.Telemetry.Type = TracingHelper.EventGridEventsPublisherActivityType;

            await next(domainEventWrappers, cancellationToken).ConfigureAwait(false);

            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;

            if (!operation.Telemetry.Properties.ContainsKey(ApplicationInsightsConstants.Exception))
            {
                operation.Telemetry.Properties.Add(ApplicationInsightsConstants.Exception, ex.ToString());
            }

            throw;
        }
        finally
        {
            // The dependency telemetry is sent when the operation is disposed
            if (originatingActivity == null)
            {
                operation.Dispose();
            }
            else
            {
                // Attach the telemetry to the originating activity
                originatingActivity.ExecuteAsCurrentActivity(operation, static x => x.Dispose());
            }
        }
    }
}