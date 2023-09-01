using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class EventPropagationClientTests
{
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly EventPropagationClient _eventPropagationClient;
    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly PublishTestDomainEvent _domainEvent;

    public EventPropagationClientTests()
    {
        this._domainEvent = new PublishTestDomainEvent();
        this._eventPropagationPublisherOptions = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        this._eventGridPublisherClient = A.Fake<EventGridPublisherClient>(opts => opts.Strict());

        var eventGridPublisherClientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>(opts => opts.Strict());
        A.CallTo(() => eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        this._eventPropagationClient = new EventPropagationClient(
            eventGridPublisherClientFactory,
            Options.Create(this._eventPropagationPublisherOptions),
            Array.Empty<IPublishingDomainEventBehavior>());
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws<Exception>();

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() =>
        {
            return this._eventPropagationClient.PublishDomainEventAsync(new PublishTestDomainEvent(), CancellationToken.None);
        });

        Assert.Contains(this._eventPropagationPublisherOptions.TopicEndpoint, exception.Message);
        Assert.Contains(typeof(PublishTestDomainEvent).FullName!, exception.Message);
        Assert.Contains(this._eventPropagationPublisherOptions.TopicName, exception.Message);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws<Exception>();

        var domainEvents = new List<PublishTestDomainEvent> { this._domainEvent, new() };

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() =>
        {
            return this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None);
        });

        Assert.Contains(this._eventPropagationPublisherOptions.TopicEndpoint, exception.Message);
        Assert.Contains(typeof(PublishTestDomainEvent).FullName!, exception.Message);
        Assert.Contains(this._eventPropagationPublisherOptions.TopicName, exception.Message);
    }

    [Fact]
    public async Task GivenEventPropagationClient_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventAsync(this._domainEvent, CancellationToken.None);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        var domainEvents = new List<PublishTestDomainEvent> { this._domainEvent, new() };

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == domainEvents.Count),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenEventsAreSuccessfullySentWithEventGridPublisher_DataVersion_ShouldBe_1_0()
    {
        var domainEvents = new List<PublishTestDomainEvent> { this._domainEvent, new() };

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.All(e => e.DataVersion == "1.0")),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None);
    }

    [Fact]
    public async Task GivenTracingPipeline_WhenPublishDomainEventAsync_ThenPipelineHandle()
    {
        // Given
        var publisherBehavior = A.Fake<IPublishingDomainEventBehavior>();

        var publisherClient = A.Fake<EventGridPublisherClient>();
        var clientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>();
        A.CallTo(() => clientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(publisherClient);

        var propagationClient = new EventPropagationClient(
            clientFactory,
            Options.Create(this._eventPropagationPublisherOptions),
            new[] { publisherBehavior });

        // When
        await propagationClient.PublishDomainEventAsync(this._domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => publisherBehavior.HandleAsync(A<DomainEventWrapperCollection>._, A<DomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [DomainEvent("publish-test")]
    private sealed class PublishTestDomainEvent : IDomainEvent
    {
        public string Text { get; set; } = string.Empty;

        public int Number { get; set; }
    }
}