using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Workleap.DomainEventPropagation;

internal static class TelemetryClientExtensions
{
    public static IOperationHolder<DependencyTelemetry> StartActivityAwareDependencyOperation(this TelemetryClient telemetryClient, object request)
    {
        if (Activity.Current is { } activity && TracingHelper.IsEventGridActivity(activity))
        {
            // When the current activity is our own Event Grid activity created in our previous event tracing behavior,
            // then we use it to initialize the Application Insights operation.
            // The Application Insights SDK will take care of populating the parent-child relationship
            // and bridge the gap between our activity, its own internal activity and the AI operation telemetry.
            // Not doing that could cause some Application Insights AND OpenTelemetry spans to be orphans.
            var operation = telemetryClient.StartOperation<DependencyTelemetry>(activity);

            return operation;
        }

        return telemetryClient.StartOperation<DependencyTelemetry>(request.GetType().Name);
    }
}