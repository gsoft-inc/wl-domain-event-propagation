using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry.Trace;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationClientTests
{
    private readonly Mock<ITelemetryClientProvider> _telemetryClientProviderMock = new ();

    internal class PublishTestDomainEvent : IDomainEvent
    {
        public string Text { get; set; }

        public int Number { get; set; }

        public string DataVersion => "1";
    }

    [Fact]
    public async Task GivenEventPropagationClient_WhenErrorDuringPublication_ThenThrowsException()
    {
        // error will be endpoint is nothing
        var options = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        var optionsWrapper = new OptionsWrapper<EventPropagationPublisherOptions>(options);
        var eventPropagationClient = new EventPropagationClient(optionsWrapper, _telemetryClientProviderMock.Object);

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => eventPropagationClient.PublishDomainEventAsync("Subject", new PublishTestDomainEvent(), CancellationToken.None));
        Assert.Equal(options.TopicName, exception.TopicName);
        Assert.Equal("Subject", exception.Subject);
        Assert.Equal(new Uri(options.TopicEndpoint), new Uri(exception.TopicEndpoint));
        _telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        // error will be endpoint is nothing
        var options = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        var optionsWrapper = new OptionsWrapper<EventPropagationPublisherOptions>(options);
        var eventPropagationClient = new EventPropagationClient(optionsWrapper, _telemetryClientProviderMock.Object);

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => eventPropagationClient.PublishDomainEventAsync(new PublishTestDomainEvent(), CancellationToken.None));
        Assert.Equal(options.TopicName, exception.TopicName);
        Assert.Equal(typeof(PublishTestDomainEvent).FullName, exception.Subject);
        Assert.Equal(new Uri(options.TopicEndpoint), new Uri(exception.TopicEndpoint));
        _telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        // error will be endpoint is nothing
        var options = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        var optionsWrapper = new OptionsWrapper<EventPropagationPublisherOptions>(options);
        var eventPropagationClient = new EventPropagationClient(optionsWrapper, _telemetryClientProviderMock.Object);

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => eventPropagationClient.PublishDomainEventsAsync(new[] { new PublishTestDomainEvent(), new PublishTestDomainEvent() }, CancellationToken.None));
        Assert.Equal(options.TopicName, exception.TopicName);
        Assert.Equal(typeof(PublishTestDomainEvent).FullName, exception.Subject);
        Assert.Equal(new Uri(options.TopicEndpoint), new Uri(exception.TopicEndpoint));
        _telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }
}