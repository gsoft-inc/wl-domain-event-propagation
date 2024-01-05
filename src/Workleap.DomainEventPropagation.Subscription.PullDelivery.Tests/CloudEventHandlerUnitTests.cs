using AutoBogus;
using Azure.Messaging;
using FluentAssertions;
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
        var domainEventTypeRegistry = new DomainEventTypeRegistry();
        var handler = new CloudEventHandler(domainEventTypeRegistry, Enumerable.Empty<IDomainEventBehavior>(), new NullLogger<CloudEventHandler>());

        // When
        var result = await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        result.Should().Be(HandlingStatus.Rejected);
    }

    [Fact]
    public async Task Given_TypeWasRegistered_When_HandleCloudEventAsync_Then_ReturnHandled()
    {
        // Given
        var cloudEvent = GivenCloudEvent();
        var domainEventTypeRegistry = new DomainEventTypeRegistry();
        domainEventTypeRegistry.RegisterDomainEvent(typeof(SampleEvent));
        var handler = new CloudEventHandler(domainEventTypeRegistry, Enumerable.Empty<IDomainEventBehavior>(), new NullLogger<CloudEventHandler>());

        // When
        var result = await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        result.Should().Be(HandlingStatus.Handled);
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

    [DomainEvent(SampleCloudEventTypeName, EventSchema.CloudEvent)]
    private class SampleEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }
}