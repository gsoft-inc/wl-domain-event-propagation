using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace Workleap.DomainEventPropagation;

internal sealed class ApplicationInsightsSubscriptionDomainEventBehavior(TelemetryClient? telemetryClient) : IDomainEventBehavior
{
    private readonly TelemetryClient? _telemetryClient = telemetryClient;

    public Task<EventProcessingStatus> HandleAsync(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next(domainEventWrapper, cancellationToken) : this.HandleWithTelemetry(domainEventWrapper, next, cancellationToken);
    }

    private async Task<EventProcessingStatus> HandleWithTelemetry(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken)
    {
        var operation = this._telemetryClient!.StartActivityAwareDependencyOperation(
            domainEventWrapper.DomainEventName,
            TracingHelper.CloudEventsSubscriberActivityType);

        if (domainEventWrapper.TryGetMetadata(ApplicationInsightsConstants.ParentOperationIdField, out var parentOperationId))
        {
            if (!operation.Telemetry.Properties.ContainsKey(ApplicationInsightsConstants.LinkedOperation))
            {
                operation.Telemetry.Properties.Add(ApplicationInsightsConstants.LinkedOperation, parentOperationId);
            }
        }

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;
        
        try
        {
            var result = await next(domainEventWrapper, cancellationToken).ConfigureAwait(false);

            operation.Telemetry.Success = true;
            return result;
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