using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public abstract class EventPropagationClientTests
{
    internal const string TopicEndpoint = "http://topicEndpoint.io";
    internal const string CloudEventName = "cloud-event";
    internal const string EventGridEventName = "eventgrid-event";

    internal readonly EventGridPublisherClient EventGridPublisherClient = A.Fake<EventGridPublisherClient>();
    internal readonly IAzureClientFactory<EventGridPublisherClient> EventGridPublisherClientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>();
    internal readonly EventGridClient EventGridClient = A.Fake<EventGridClient>();
    internal readonly IAzureClientFactory<EventGridClient> EventGridClientFactory = A.Fake<IAzureClientFactory<EventGridClient>>();
    internal readonly EventPropagationClient EventPropagationClient;

    private readonly IOptions<EventPropagationPublisherOptions> _publisherOptions;

    protected EventPropagationClientTests(IOptions<EventPropagationPublisherOptions> publisherOptions)
    {
        this._publisherOptions = publisherOptions;
        A.CallTo(() => this.EventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.CustomTopicClientName)).Returns(this.EventGridPublisherClient);
        A.CallTo(() => this.EventGridClientFactory.CreateClient(EventPropagationPublisherOptions.NamespaceTopicClientName)).Returns(this.EventGridClient);

        this.EventPropagationClient = new EventPropagationClient(
            this.EventGridPublisherClientFactory,
            this.EventGridClientFactory,
            publisherOptions,
            Array.Empty<IPublishingDomainEventBehavior>());
    }

    [Fact]
    public async Task GivenNullDomainEvent_WhenPublishDomainEvent_ThenArgumentNullException()
    {
        // Given
        var domainEvent = (IEnumerable<TestEventGridEvent>)null!;

        // When
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await this.EventPropagationClient.PublishDomainEventsAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Equal("domainEvents", exception.ParamName);
    }

    [Fact]
    public async Task GivenEmptyDomainEventsCollections_WhenPublishDomainEvents_ThenNothing()
    {
        // Given
        var events = Array.Empty<TestEventGridEvent>();

        // When
        await this.EventPropagationClient.PublishDomainEventsAsync(events, CancellationToken.None);

        // Then
        A.CallTo(() => this.EventGridPublisherClient.SendEventsAsync(A<IEnumerable<EventGridEvent>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenAdditionalBehavior_WhenPublishEventGridEvent_ThenPipelineHandle()
    {
        // Given
        var domainEvent = new TestEventGridEvent() { Text = "Hello world", Number = 1 };
        var publisherBehavior = A.Fake<IPublishingDomainEventBehavior>();

        var propagationClient = new EventPropagationClient(
            this.EventGridPublisherClientFactory,
            this.EventGridClientFactory,
            this._publisherOptions,
            new[] { publisherBehavior });

        // When
        await propagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => publisherBehavior.HandleAsync(A<DomainEventWrapperCollection>._, A<DomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task GivenAdditionalBehavior_WhenPublishCloudEvent_ThenPipelineHandle()
    {
        // Given
        var domainEvent = new TestCloudEvent() { Text = "Hello world", Number = 2 };
        var publisherBehavior = A.Fake<IPublishingDomainEventBehavior>();

        var propagationClient = new EventPropagationClient(
            this.EventGridPublisherClientFactory,
            this.EventGridClientFactory,
            this._publisherOptions,
            new[] { publisherBehavior });

        // When
        await propagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => publisherBehavior.HandleAsync(A<DomainEventWrapperCollection>._, A<DomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    protected static bool IsSingleEventGridEvent(IEnumerable<EventGridEvent> events)
    {
        return events.Single() is
        {
            Subject: EventGridEventName,
            EventType: EventGridEventName,
            DataVersion: "1.0",
        };
    }

    protected static bool IsSingleCloudEvent(IEnumerable<CloudEvent> events)
    {
        return events.Single() is
        {
            Type: CloudEventName,
            Source: TopicEndpoint,
        };
    }

    [DomainEvent(EventGridEventName, EventSchema.EventGridEvent)]
    protected sealed class TestEventGridEvent : IDomainEvent
    {
        public string Text { get; set; } = string.Empty;

        public int Number { get; set; }
    }

    [DomainEvent(CloudEventName, EventSchema.CloudEvent)]
    protected sealed class TestCloudEvent : IDomainEvent
    {
        public string Text { get; set; } = string.Empty;

        public int Number { get; set; }
    }
}

public class EventPropagationClientForCustomTopicTests : EventPropagationClientTests
{
    private static readonly IOptions<EventPropagationPublisherOptions> PublisherOptions = Options.Create(new EventPropagationPublisherOptions()
    {
        TopicType = TopicType.Custom,
        TopicEndpoint = TopicEndpoint,
        TopicAccessKey = "topicAccessKey",
    });

    public EventPropagationClientForCustomTopicTests() : base(PublisherOptions)
    {
    }

    [Fact]
    public async Task GivenEventGridEvent_WhenPublishEvent_ThenCallsPropagationClient()
    {
        // Given
        var domainEvent = new TestEventGridEvent() { Text = "Hello world", Number = 1 };

        // When
        await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => this.EventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => IsSingleEventGridEvent(events)),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task GivenCloudEvent_WhenPublishEvent_ThenCallsPropagationClient()
    {
        // Given
        var domainEvent = new TestCloudEvent() { Text = "Hello world", Number = 1 };

        // When
        await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => this.EventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<CloudEvent>>.That.Matches(events => IsSingleCloudEvent(events)),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task GivenEventGridEvent_WhenErrorDuringPublishEvent_ThenThrowsException()
    {
        // Given
        var domainEvent = new TestEventGridEvent() { Text = "Hello world", Number = 1 };
        A.CallTo(() => this.EventGridPublisherClient.SendEventsAsync(A<IEnumerable<EventGridEvent>>._, A<CancellationToken>._)).Throws<Exception>();

        // When
        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(async () => await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Contains(EventGridEventName, exception.Message);
    }

    [Fact]
    public async Task GivenCloudEvent_WhenErrorDuringPublishEvent_ThenThrowsException()
    {
        // Given
        var domainEvent = new TestCloudEvent() { Text = "Hello world", Number = 2 };
        A.CallTo(() => this.EventGridPublisherClient.SendEventsAsync(A<IEnumerable<CloudEvent>>._, A<CancellationToken>._)).Throws<Exception>();

        // When
        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(async () => await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Contains(CloudEventName, exception.Message);
    }
}

public class EventPropagationClientForNamespaceTopicTests : EventPropagationClientTests
{
    private const string TopicName = "topicName";

    private static readonly IOptions<EventPropagationPublisherOptions> PublisherOptions = Options.Create(new EventPropagationPublisherOptions()
    {
        TopicType = TopicType.Namespace,
        TopicName = TopicName,
        TopicEndpoint = TopicEndpoint,
        TopicAccessKey = "topicAccessKey",
    });

    public EventPropagationClientForNamespaceTopicTests() : base(PublisherOptions)
    {
    }

    [Fact]
    public async Task GivenEventGridEvent_WhenPublishDomainEvent_ThenThrowsException()
    {
        // Given
        var domainEvent = new TestEventGridEvent() { Text = "Hello world", Number = 1 };

        // When
        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(async () => await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Contains(EventGridEventName, exception.Message);
    }

    [Fact]
    public async Task GivenCloudEvent_WhenPublishEvent_ThenCallsPropagationClient()
    {
        // Given
        var domainEvent = new TestCloudEvent() { Text = "Hello world", Number = 1 };

        // When
        await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => this.EventGridClient.PublishCloudEventsAsync(
                TopicName,
                A<IEnumerable<CloudEvent>>.That.Matches(events => IsSingleCloudEvent(events)),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task GivenCloudEvent_WhenErrorDuringPublishEvent_ThenThrowsException()
    {
        // Given
        var domainEvent = new TestCloudEvent() { Text = "Hello world", Number = 2 };
        A.CallTo(() => this.EventGridClient.PublishCloudEventsAsync(PublisherOptions.Value.TopicName, A<IEnumerable<CloudEvent>>._, A<CancellationToken>._)).Throws<Exception>();

        // When
        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(async () => await this.EventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Contains(CloudEventName, exception.Message);
    }
}