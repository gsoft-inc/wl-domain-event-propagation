using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace Workleap.DomainEventPropagation;

internal class SubscriptionApplicationInsightsTracingBehavior : ISubscriptionDomainEventBehavior
{
    private readonly TelemetryClient? _telemetryClient;

    public SubscriptionApplicationInsightsTracingBehavior(TelemetryClient? telemetryClient)
    {
        this._telemetryClient = telemetryClient;
    }

    public Task Handle(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next(domainEvent) : this.HandleWithTelemetry(domainEvent, next, cancellationToken);
    }

    private async Task HandleWithTelemetry(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken)
    {
        var operation = this._telemetryClient!.StartActivityAwareDependencyOperation(domainEvent);

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

        try
        {
            operation.Telemetry.Name = TracingHelper.EventGridEventsSubscriberActivityName;
            operation.Telemetry.Type = ApplicationInsightsConstants.ConsumerTelemetryKind;
            await next(domainEvent).ConfigureAwait(false);
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