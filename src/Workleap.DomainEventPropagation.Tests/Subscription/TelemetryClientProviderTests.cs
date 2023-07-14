using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class TelemetryClientProviderTests
{
    private readonly TelemetryClientProvider _telemetryClientProvider;

    public TelemetryClientProviderTests()
    {
        // TODO: Remove when getting rid of application insights
        var mockTelemetryChannel = new MockTelemetryChannel();
        var telemetryClient = GetTestTelemetryClient(mockTelemetryChannel);
        this._telemetryClientProvider = new TelemetryClientProvider(telemetryClient);

        var activitySource = new ActivitySource("ActivitySource");
        var activityListener = new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        activitySource.StartActivity("Activity");
    }

    [Fact]
    public void GivenSpanAttributes_WhenTrackingEvent_ThenEventIsTrackedWithAttributes()
    {
        // Given
        var spanName = "name";
        var message = "message";
        var eventType = "eventType";

        // When
        this._telemetryClientProvider.TrackEvent(spanName, message, eventType);

        // Then
        var spanAttributes = Activity.Current?.Events.SingleOrDefault(x => x.Name == spanName).Tags;

        Assert.NotNull(spanAttributes);
        Assert.True(spanAttributes.Any());
        Assert.Equal(message, spanAttributes?.Single(x => x.Key == "app.message").Value);
        Assert.Equal(eventType, spanAttributes?.Single(x => x.Key == "app.eventType").Value);
    }

    [Fact]
    public void GivenException_WhenTrackingException_ThenExceptionIsTrackedWithAttributes()
    {
        // Given
        var message = "CoolException";
        var exception = new Exception(message);

        // When
        this._telemetryClientProvider.TrackException(exception);

        // Then
        var spanAttributes = Activity.Current?.Events.SingleOrDefault(x => x.Name == "exception").Tags;

        Assert.NotNull(spanAttributes);
        Assert.True(spanAttributes.Any());
        Assert.Equal(nameof(Exception), spanAttributes?.Single(x => x.Key == "exception.type").Value);
        Assert.Equal("System.Exception: CoolException", spanAttributes?.Single(x => x.Key == "exception.stacktrace").Value);
        Assert.Equal(message, spanAttributes?.Single(x => x.Key == "exception.message").Value);
    }

    [Fact]
    public void GivenNullTelemetryClient_WhenTrackingEvent_ThenNothingHappens()
    {
        // Given
        var telemetryClientProvider = new TelemetryClientProvider(null);

        // When
        telemetryClientProvider.TrackEvent("Goodbye", "Goodbye", "Goodbye");
    }

    [Fact]
    public void GivenNullRequestTelemetry_WhenStartingOperation_ThenNothingHappens()
    {
        // Given
        var mockTelemetryChannel = new MockTelemetryChannel();
        var telemetryClient = GetTestTelemetryClient(mockTelemetryChannel);
        var telemetryClientProvider = new TelemetryClientProvider(telemetryClient);

        // When
        var operation = telemetryClientProvider.StartOperation((RequestTelemetry)null);
        Assert.Null(operation);
    }

    [Fact]
    public void GivenNullDependencyTelemetry_WhenStartingOperation_ThenNothingHappens()
    {
        // Given
        var mockTelemetryChannel = new MockTelemetryChannel();
        var telemetryClient = GetTestTelemetryClient(mockTelemetryChannel);
        var telemetryClientProvider = new TelemetryClientProvider(telemetryClient);

        // When
        var operation = telemetryClientProvider.StartOperation((DependencyTelemetry)null);
        Assert.Null(operation);
    }

    [Fact]
    public void GivenNullTelemetryClient_WhenTrackingException_ThenNothingHappens()
    {
        // Given
        var telemetryClientProvider = new TelemetryClientProvider(null);

        // When
        telemetryClientProvider.TrackException(new Exception("Cool exception"));
    }

    [Fact]
    public void GivenTelemetryClient_WhenGettingOperationId_ThenEventIsTracked()
    {
        // Given
        var telemetryClient = GetTestTelemetryClient();
        telemetryClient.StartOperation<DependencyTelemetry>("CoolOperation");

        // When
        var telemetryClientProvider = new TelemetryClientProvider(telemetryClient);
        var operationId = telemetryClientProvider.GetOperationId();

        // Then
        Assert.NotNull(operationId);
    }

    private static TelemetryClient GetTestTelemetryClient(ITelemetryChannel telemetryChannel = null)
    {
        if (telemetryChannel == null)
        {
            telemetryChannel = new MockTelemetryChannel();
        }

        var telemetryConfiguration = new TelemetryConfiguration
        {
            TelemetryChannel = telemetryChannel,
            InstrumentationKey = Guid.NewGuid().ToString()
        };

        telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
        var telemetryClient = new TelemetryClient(telemetryConfiguration);

        return telemetryClient;
    }
}