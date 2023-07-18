using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using OpenTelemetry.Trace;

namespace Workleap.DomainEventPropagation;

// The TelemetryClientProvider manages the fact that the package could be used in a service without Application insights telemetry
// In which case the telemetryClient would be null
// https://docs.microsoft.com/en-us/azure/azure-monitor/app/custom-operations-tracking
public sealed class TelemetryClientProvider : ITelemetryClientProvider
{
    private readonly TelemetryClient _telemetryClient;

    public TelemetryClientProvider(TelemetryClient telemetryClient = default(TelemetryClient))
    {
        this._telemetryClient = telemetryClient;
    }

    public TelemetrySpan TrackEvent(string name, string message, string eventType, TelemetrySpan span = null)
    {
        span ??= Tracer.CurrentSpan;

        var spanAttributes = new SpanAttributes();
        spanAttributes.Add("app.message", message);
        spanAttributes.Add("app.eventType", eventType);

        return span.AddEvent(name, spanAttributes);
    }

    public void TrackException(Exception exception, TelemetrySpan span = null)
    {
        span ??= Tracer.CurrentSpan;

        span.SetStatus(Status.Error);
        span.RecordException(exception);
    }

    // TODO: Replace by OTel
    public IOperationHolder<T> StartOperation<T>(T telemetry) where T : OperationTelemetry
    {
        if (this._telemetryClient != null && telemetry != null)
        {
            return this._telemetryClient.StartOperation(telemetry);
        }

        return null;
    }

    // TODO: Replace by OTel
    public void StopOperation<T>(IOperationHolder<T> operation) where T : OperationTelemetry
    {
        if (this._telemetryClient != null && operation != null)
        {
            this._telemetryClient.StopOperation(operation);
        }
    }
}