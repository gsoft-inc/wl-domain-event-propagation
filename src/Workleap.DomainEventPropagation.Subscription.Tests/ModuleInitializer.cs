using System.Runtime.CompilerServices;
using OpenTelemetry.Context.Propagation;

namespace Workleap.DomainEventPropagation.Subscription.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void InitializeTelemetry()
    {
        OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new BaggagePropagator(),
        }));
    }
}