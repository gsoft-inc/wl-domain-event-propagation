using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class EventPropagationClientTests
{
    private const string DomainEventName = "publish-test";

    private readonly IOptions<EventPropagationPublisherOptions> _publisherOptions = A.Fake<IOptions<EventPropagationPublisherOptions>>();
    private readonly EventGridPublisherClient _eventGridPublisherClient = A.Fake<EventGridPublisherClient>();
    private readonly IAzureClientFactory<EventGridPublisherClient> _eventGridPublisherClientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>();

    private readonly EventPropagationClient _eventPropagationClient;

    public EventPropagationClientTests()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        this._eventPropagationClient = new EventPropagationClient(
            this._eventGridPublisherClientFactory,
            this._publisherOptions,
            Array.Empty<IPublishingDomainEventBehavior>());
    }

    [Fact]
    public async Task GivenNullDomainEvent_WhenPublishDomainEvent_ThenArgumentNullException()
    {
        // Given
        var domainEvent = (IEnumerable<PublishTestDomainEvent>)null!;

        // When
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await this._eventPropagationClient.PublishDomainEventsAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Equal("domainEvents", exception.ParamName);
    }

    [Fact]
    public async Task GivenEmptyDomainEventsCollections_WhenPublishDomainEvents_ThenNothing()
    {
        // Given
        var events = Array.Empty<PublishTestDomainEvent>();

        // When
        await this._eventPropagationClient.PublishDomainEventsAsync(events, CancellationToken.None);

        // Then
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(A<IEnumerable<EventGridEvent>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenTracingPipeline_WhenPublishDomainEvent_ThenPipelineHandle()
    {
        // Given
        var domainEvent = new PublishTestDomainEvent();
        var publisherBehavior = A.Fake<IPublishingDomainEventBehavior>();

        var propagationClient = new EventPropagationClient(
            this._eventGridPublisherClientFactory,
            this._publisherOptions,
            new[] { publisherBehavior });

        // When
        await propagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => publisherBehavior.HandleAsync(A<DomainEventWrapperCollection>._, A<DomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task GivenDomainEvent_WhenErrorDuringPublishDomainEvent_ThenThrowsException()
    {
        // Given
        var domainEvent = new PublishTestDomainEvent();
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(A<IEnumerable<EventGridEvent>>._, A<CancellationToken>._)).Throws<Exception>();

        // When
        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(async () => await this._eventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None));

        // Then
        Assert.Contains(DomainEventName, exception.Message);
    }

    [Fact]
    public async Task GivenDomainEvent_WhenErrorDuringPublishDomainEvent_Then()
    {
        // Given
        var domainEvent = new PublishTestDomainEvent();

        // When
        await this._eventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(evt => evt.Count() == 1 && evt.First().EventType == DomainEventName),
                A<CancellationToken>._))
            .MustHaveHappened();
    }



    [DomainEvent(DomainEventName)]
    private sealed class PublishTestDomainEvent : IDomainEvent
    {
        public string Text { get; set; } = string.Empty;

        public int Number { get; set; }
    }
}