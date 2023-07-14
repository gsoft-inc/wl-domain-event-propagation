using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using OpenTelemetry.Trace;

namespace Workleap.DomainEventPropagation;

public interface ITelemetryClientProvider
{
    IOperationHolder<T> StartOperation<T>(T telemetry) where T : OperationTelemetry;

    void StopOperation<T>(IOperationHolder<T> operation) where T : OperationTelemetry;

    TelemetrySpan TrackEvent(string name, string message, string eventType, TelemetrySpan span = null);

    void TrackException(Exception exception, TelemetrySpan span = null);

    string GetOperationId();
}