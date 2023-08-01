using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationClientTests
{
    private const string Subject = nameof(Subject);

    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly EventPropagationClient _eventPropagationClient;
    private readonly IAzureClientFactory<EventGridPublisherClient> _eventGridPublisherClientFactory;
    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly PublishTestDomainEvent _domainEvent;

    internal class PublishTestDomainEvent : IDomainEvent
    {
        public string Text { get; set; }

        public int Number { get; set; }

        public string DataVersion => "1";
    }

    public EventPropagationClientTests()
    {
        this._domainEvent = new PublishTestDomainEvent();
        this._eventPropagationPublisherOptions = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        this._eventGridPublisherClientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>(opts => opts.Strict());
        this._eventGridPublisherClient = A.Fake<EventGridPublisherClient>(opts => opts.Strict());

        this._eventPropagationClient = new EventPropagationClient(this._eventGridPublisherClientFactory, Options.Create(this._eventPropagationPublisherOptions));
    }

    [Fact]
    public async Task GivenEventPropagationClient_WhenErrorDuringPublication_ThenThrowsEventPropagationPublishingException()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws(A.Fake<Exception>());

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => this._eventPropagationClient.PublishDomainEventAsync(Subject, this._domainEvent, CancellationToken.None)).ConfigureAwait(false);

        Assert.Equal(this._eventPropagationPublisherOptions.TopicEndpoint, exception.TopicEndpoint);
        Assert.Equal(Subject, exception.Subject);
        Assert.Equal(this._eventPropagationPublisherOptions.TopicName, exception.TopicName);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws(A.Fake<Exception>());

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => this._eventPropagationClient.PublishDomainEventAsync(new PublishTestDomainEvent(), CancellationToken.None)).ConfigureAwait(false);

        Assert.Equal(this._eventPropagationPublisherOptions.TopicEndpoint, exception.TopicEndpoint);
        Assert.Equal(typeof(PublishTestDomainEvent).FullName, exception.Subject);
        Assert.Equal(this._eventPropagationPublisherOptions.TopicName, exception.TopicName);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws(A.Fake<Exception>());

        var domainEvents = new List<PublishTestDomainEvent> { this._domainEvent, new() };

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None)).ConfigureAwait(false);

        Assert.Equal(this._eventPropagationPublisherOptions.TopicEndpoint, exception.TopicEndpoint);
        Assert.Equal(typeof(PublishTestDomainEvent).FullName, exception.Subject);
        Assert.Equal(this._eventPropagationPublisherOptions.TopicName, exception.TopicName);
    }

    [Fact]
    public async Task GivenEventPropagationClient_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventAsync(Subject, this._domainEvent, CancellationToken.None).ConfigureAwait(false);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventAsync_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventAsync(this._domainEvent, CancellationToken.None).ConfigureAwait(false);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        var domainEvents = new List<PublishTestDomainEvent> { this._domainEvent, new() };

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == domainEvents.Count),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None).ConfigureAwait(false);
    }
}