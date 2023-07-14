using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

// Info: https://medium.com/@jrolstad/unit-testing-and-microsoft-application-insights-6db0929b39e6
public class MockTelemetryChannel : ITelemetryChannel
{
    public ConcurrentBag<ITelemetry> SentTelemtries = new ConcurrentBag<ITelemetry>();
    public bool IsFlushed { get; private set; }
    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }

    public void Send(ITelemetry item)
    {
        this.SentTelemtries.Add(item);
    }

    public void Flush()
    {
        this.IsFlushed = true;
    }

    public void Dispose()
    {

    }
}