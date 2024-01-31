using Azure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class CloudEventHandlerUnitTests
{
    private const string SampleCloudEventTypeName = "sample-cloud-event";

    [Fact]
    public async Task Given_EventTypeNotRegistered_When_HandleCloudEventAsync_Then_ReturnRejected()
    {
        // Given
        var cloudEvent = GivenCloudEvent();
        var handler = GivenCloudEventHandler();

        // When
        var result = await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        result.Should().Be(EventProcessingStatus.Rejected);
    }

    [Fact]
    public async Task Given_TypeWasRegistered_When_HandleCloudEventAsync_Then_ReturnHandled()
    {
        // Given
        var cloudEvent = GivenCloudEvent();
        var domainEventTypeRegistry = new DomainEventTypeRegistry();
        domainEventTypeRegistry.RegisterDomainEvent(typeof(SampleEvent));
        var handler = GivenCloudEventHandler(domainEventTypeRegistry);

        // When
        var result = await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        result.Should().Be(EventProcessingStatus.Handled);
    }

    private static CloudEvent GivenCloudEvent()
    {
        var wrapper = DomainEventWrapper.Wrap(new SampleEvent());
        var cloudEvent = new CloudEvent(
            type: wrapper.DomainEventName,
            source: "http://source.com",
            jsonSerializableData: wrapper.Data);
        return cloudEvent;
    }

    private static ICloudEventHandler GivenCloudEventHandler(DomainEventTypeRegistry? registry = null)
    {
        var services = new ServiceCollection();
        return new CloudEventHandler(
            services.BuildServiceProvider(),
            registry ?? new DomainEventTypeRegistry(),
            Enumerable.Empty<IDomainEventBehavior>(),
            new NullLogger<ICloudEventHandler>());
    }

    [DomainEvent(SampleCloudEventTypeName, EventSchema.CloudEvent)]
    private class SampleEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }
}