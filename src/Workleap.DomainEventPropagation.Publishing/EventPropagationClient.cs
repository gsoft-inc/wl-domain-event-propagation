using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
/// </summary>
internal sealed class EventPropagationClient : IEventPropagationClient
{
    private const string DomainEventDefaultVersion = "1.0";
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly IAzureClientFactory<EventGridPublisherClient> _eventGridPublisherClientFactory;
    private readonly IServiceProvider _serviceProvider;

    public EventPropagationClient(
        IAzureClientFactory<EventGridPublisherClient> eventGridPublisherClientFactory,
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions,
        IServiceProvider serviceProvider)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._eventGridPublisherClientFactory = eventGridPublisherClientFactory;
        this._serviceProvider = serviceProvider;
    }

    private string TopicName => this._eventPropagationPublisherOptions.TopicName;

    public Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.PublishDomainEventsAsync(new[] { domainEvent }, cancellationToken);

    public async Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken)
        where T : IDomainEvent
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        try
        {
            async Task Handler(IEnumerable<IDomainEvent> events)
            {
                var eventGridEvents = this.GetEventsList(events);
                await this._eventGridPublisherClientFactory
                    .CreateClient(EventPropagationPublisherOptions.ClientName)
                    .SendEventsAsync(eventGridEvents, cancellationToken)
                    .ConfigureAwait(false);
            }

            var accumulator = this._serviceProvider
                .GetServices<IPublishingDomainEventBehavior>()
                .Reverse()
                .Aggregate(
                    Handler,
                    (DomainEventsHandlerDelegate next, IPublishingDomainEventBehavior pipeline) => (events) => pipeline.Handle(events, next, cancellationToken));

            await accumulator((IEnumerable<IDomainEvent>)domainEvents).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new EventPropagationPublishingException("An error occured while publishing events to EventGrid", ex, this.TopicName, typeof(T).FullName!, this._eventPropagationPublisherOptions.TopicEndpoint);
        }
    }

    private IEnumerable<EventGridEvent> GetEventsList<T>(IEnumerable<T> domainEvents) where T : IDomainEvent
    {
        return domainEvents.Select(domainEvent => new EventGridEvent(
            subject: $"{this.TopicName}-{typeof(T).FullName!}",
            eventType: domainEvent.GetType().AssemblyQualifiedName,
            dataVersion: DomainEventDefaultVersion,
            data: new BinaryData(domainEvent, SerializerOptions)));
    }
}