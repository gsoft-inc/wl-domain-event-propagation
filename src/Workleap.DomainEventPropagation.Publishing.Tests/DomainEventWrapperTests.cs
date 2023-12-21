using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class DomainEventWrapperTests
{
    private const string EventGridDomainEventName = "sample-eg-event";
    private const string CloudEventDomainEventName = "sample-cloud-event";

    private readonly EventGridEvent _eventGridEvent = new(
        "subject",
        "eventType",
        "1.0",
        new BinaryData(new SampleDomainEvent()));

    private readonly CloudEvent _cloudEvent = new(
        "source",
        "eventType",
        new BinaryData(new CloudEventSampleDomainEvent()),
        nameof(CloudEventSampleDomainEvent));

    [Fact]
    public void GivenEventGridEvent_WhenSetMetadata_ThenMetadataIsSet()
    {
        // Given
        var eventWrapper = new DomainEventWrapper(this._eventGridEvent);

        // When
        eventWrapper.SetMetadata("someKey", "someValue");

        // Then
        var valueFound = eventWrapper.TryGetMetadata("someKey", out var value);

        Assert.True(valueFound);
        Assert.Equal("someValue", value);
    }

    [Fact]
    public void GivenCloudEvent_WhenSetMetadata_ThenNothingOccurs()
    {
        // Given
        var eventWrapper = new DomainEventWrapper(this._cloudEvent);

        // When
        eventWrapper.SetMetadata("someKey", "someValue");

        // Then
        var valueFound = eventWrapper.TryGetMetadata("someKey", out var value);

        Assert.False(valueFound);
        Assert.Null(value);
    }

    [Fact]
    public void GivenEventGridEventWithoutMetadata_WhenTryGetMetadata_ThenMetadataNotFound()
    {
        // Given
        var eventWrapper = new DomainEventWrapper(this._eventGridEvent);

        // When
        var valueFound = eventWrapper.TryGetMetadata("somekey", out var value);

        // Then
        Assert.False(valueFound);
        Assert.Null(value);
    }

    [Fact]
    public void GivenDomainEvent_WhenWrapEvent_ThenEventWrappedProperly()
    {
        // Given
        var domainEvent = new SampleDomainEvent() { Message = "Hello world" };

        // When
        var wrappedEvent = DomainEventWrapper.Wrap(domainEvent);

        // Then
        Assert.Equal(EventGridDomainEventName, wrappedEvent.DomainEventName);
        Assert.Equal(EventSchema.EventGridEvent, wrappedEvent.DomainEventSchema);

        var unwrappedEvent = (SampleDomainEvent)wrappedEvent.Unwrap(typeof(SampleDomainEvent));

        Assert.NotNull(unwrappedEvent);
        Assert.Equal(domainEvent.Message, unwrappedEvent.Message);
    }

    [Fact]
    public void GivenDomainCloudEvent_WhenWrapEvent_ThenEventWrappedProperly()
    {
        // Given
        var domainEvent = new CloudEventSampleDomainEvent() { Message = "Hello world" };

        // When
        var wrappedEvent = DomainEventWrapper.Wrap(domainEvent);

        // Then
        Assert.Equal(CloudEventDomainEventName, wrappedEvent.DomainEventName);
        Assert.Equal(EventSchema.CloudEvent, wrappedEvent.DomainEventSchema);

        var unwrappedEvent = (CloudEventSampleDomainEvent)wrappedEvent.Unwrap(typeof(CloudEventSampleDomainEvent));

        Assert.NotNull(unwrappedEvent);
        Assert.Equal(domainEvent.Message, unwrappedEvent.Message);
    }

    [DomainEvent(EventGridDomainEventName)]
    private class SampleDomainEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }

    [DomainEvent(CloudEventDomainEventName, EventSchema.CloudEvent)]
    private class CloudEventSampleDomainEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }
}