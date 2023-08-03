using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
/// </summary>
internal sealed class EventPropagationClient : IEventPropagationClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly IAzureClientFactory<EventGridPublisherClient> _eventGridPublisherClientFactory;

    public EventPropagationClient(
        IAzureClientFactory<EventGridPublisherClient> eventGridPublisherClientFactory,
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._eventGridPublisherClientFactory = eventGridPublisherClientFactory;
    }

    private string TopicName => this._eventPropagationPublisherOptions.TopicName;

    public Task PublishDomainEventAsync<T>(string subject, T domainEvent, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.PublishDomainEventsAsync(subject, new[] { domainEvent }, cancellationToken);

    public Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.PublishDomainEventAsync(typeof(T).FullName!, domainEvent, cancellationToken);

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.PublishDomainEventsAsync(typeof(T).FullName!, domainEvents, cancellationToken);

    public async Task PublishDomainEventsAsync<T>(string subject, IEnumerable<T> domainEvents, CancellationToken cancellationToken)
        where T : IDomainEvent
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        try
        {
            var eventGridEvents = this.GetEventsList(subject, domainEvents);

            await this._eventGridPublisherClientFactory
                .CreateClient(EventPropagationPublisherOptions.ClientName)
                .SendEventsAsync(eventGridEvents, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new EventPropagationPublishingException("An error occured while publishing events to EventGrid", ex, this.TopicName, subject, this._eventPropagationPublisherOptions.TopicEndpoint);
        }
    }

    private IEnumerable<EventGridEvent> GetEventsList<T>(string subject, IEnumerable<T> domainEvents) where T : IDomainEvent
    {
        // TODO: Propagate correlation ID by setting data with "telemetryCorrelationId" property when OpenTelemetry is fully supported
        return domainEvents.Select(domainEvent => new EventGridEvent(
            subject: $"{this.TopicName}-{subject}",
            eventType: domainEvent.GetType().FullName,
            dataVersion: domainEvent.DataVersion,
            data: new BinaryData(domainEvent, SerializerOptions)));
    }
}