using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class DomainEventWrapperTests
{
    private readonly EventGridEvent _eventGridEvent = new EventGridEvent(
        "subject",
        "eventType",
        "1.0",
        new BinaryData(new SampleDomainEvent()));

    private const string DomainEventName = "sample-event";

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
        Assert.Equal(DomainEventName, wrappedEvent.DomainEventName);

        var unwrappedEvent = (SampleDomainEvent)wrappedEvent.Unwrap(typeof(SampleDomainEvent));

        Assert.NotNull(unwrappedEvent);
        Assert.Equal(domainEvent.Message, unwrappedEvent.Message);
    }

    [DomainEvent(DomainEventName)]
    private class SampleDomainEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }
}