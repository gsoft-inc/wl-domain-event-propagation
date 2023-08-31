using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace Workleap.DomainEventPropagation;

internal class PublishigApplicationInsightsTracingBehavior : IPublishingDomainEventBehavior
{
    private readonly TelemetryClient? _telemetryClient;

    public PublishigApplicationInsightsTracingBehavior(TelemetryClient? telemetryClient)
    {
        this._telemetryClient = telemetryClient;
    }

    public Task Handle(IEnumerable<DomainEventWrapper> events, DomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next(events) : this.HandleWithTelemetry(events, next);
    }

    private async Task HandleWithTelemetry(IEnumerable<DomainEventWrapper> events, DomainEventsHandlerDelegate next)
    {
        var operation = this._telemetryClient!.StartActivityAwareDependencyOperation(events);

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

        try
        {
            operation.Telemetry.Name = TracingHelper.EventGridEventsPublisherActivityName;
            operation.Telemetry.Type = ApplicationInsightsConstants.ProducerTelemetryKind;
            await next(events).ConfigureAwait(false);
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