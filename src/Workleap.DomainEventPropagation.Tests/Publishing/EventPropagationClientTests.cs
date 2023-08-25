using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationClientTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly EventPropagationClient _eventPropagationClient;
    private readonly IAzureClientFactory<EventGridPublisherClient> _eventGridPublisherClientFactory;
    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly PublishTestDomainEvent _domainEvent;

    public EventPropagationClientTests()
    {
        this._domainEvent = new PublishTestDomainEvent();
        this._eventPropagationPublisherOptions = new EventPropagationPublisherOptions { TopicName = "Organization", TopicAccessKey = "AccessKey", TopicEndpoint = "http://localhost:11111" };
        this._eventGridPublisherClientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>(opts => opts.Strict());
        this._eventGridPublisherClient = A.Fake<EventGridPublisherClient>(opts => opts.Strict());
        this._serviceProvider = A.Fake<IServiceProvider>();

        this._eventPropagationClient = new EventPropagationClient(
            this._eventGridPublisherClientFactory,
            Options.Create(this._eventPropagationPublisherOptions),
            this._serviceProvider);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventAsync_WhenErrorDuringPublication_ThenThrowsException()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Throws(A.Fake<Exception>());

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => this._eventPropagationClient.PublishDomainEventAsync(new PublishTestDomainEvent(), CancellationToken.None));

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

        var exception = await Assert.ThrowsAsync<EventPropagationPublishingException>(() => this._eventPropagationClient.PublishDomainEventsAsync(domainEvents, CancellationToken.None));

        Assert.Equal(this._eventPropagationPublisherOptions.TopicEndpoint, exception.TopicEndpoint);
        Assert.Equal(typeof(PublishTestDomainEvent).FullName, exception.Subject);
        Assert.Equal(this._eventPropagationPublisherOptions.TopicName, exception.TopicName);
    }

    [Fact]
    public async Task GivenEventPropagationClient_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);
        A.CallTo(() => this._serviceProvider.GetService(A<Type>._)).Returns(Array.Empty<IPublishingDomainEventBehavior>());

        A.CallTo(() => this._eventGridPublisherClient.SendEventsAsync(
                A<IEnumerable<EventGridEvent>>.That.Matches(events => events.Count() == 1),
                A<CancellationToken>._))
            .Returns(Task.FromResult(A.Fake<Azure.Response>()));

        await this._eventPropagationClient.PublishDomainEventAsync(this._domainEvent, CancellationToken.None);
    }

    [Fact]
    public async Task GivenGenericPublishDomainEventsAsync_WhenEventsAreSuccessfullySentWithEventGridPublisher_ThenNoErrors()
    {
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);
        A.CallTo(() => this._serviceProvider.GetService(A<Type>._)).Returns(Array.Empty<IPublishingDomainEventBehavior>());

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
        A.CallTo(() => this._eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(this._eventGridPublisherClient);
        A.CallTo(() => this._serviceProvider.GetService(A<Type>._)).Returns(Array.Empty<IPublishingDomainEventBehavior>());

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

        var services = new ServiceCollection();
        services.AddSingleton(publisherBehavior);
        var serviceProvider = services.BuildServiceProvider();

        var publisherClient = A.Fake<EventGridPublisherClient>();
        var clientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>();
        A.CallTo(() => clientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(publisherClient);

        var propagationClient = new EventPropagationClient(
            clientFactory,
            Options.Create(this._eventPropagationPublisherOptions),
            serviceProvider);

        // When
        await propagationClient.PublishDomainEventAsync(this._domainEvent, CancellationToken.None);

        // Then
        A.CallTo(() => publisherBehavior.Handle(A<IEnumerable<PublishTestDomainEvent>>._, A<DomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    internal class PublishTestDomainEvent : IDomainEvent
    {
        public string Text { get; set; } = string.Empty;

        public int Number { get; set; }

        public string DataVersion => "1";

        public IDictionary<string, string>? ExtensionAttributes { get; set; } = new Dictionary<string, string>();
    }
}