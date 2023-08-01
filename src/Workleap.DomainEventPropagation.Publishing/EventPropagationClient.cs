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

    public Task PublishDomainEventAsync(string subject, IDomainEvent domainEvent, CancellationToken cancellationToken)
        => this.PublishDomainEventsAsync(subject, new[] { domainEvent }, cancellationToken);

    public Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent
        => this.PublishDomainEventAsync(typeof(T).FullName, domainEvent, cancellationToken);

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken) where T : IDomainEvent
    {
        var events = domainEvents as IEnumerable<IDomainEvent>;
        if (events == null)
        {
            throw new ArgumentException("Can't cast domainEvents to IEnumerable<IDomainEvent>");
        }

        return this.PublishDomainEventsAsync(typeof(T).FullName, events, cancellationToken);
    }

    public async Task PublishDomainEventsAsync(string subject, IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
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
            var exception = new EventPropagationPublishingException("An error occured while publishing events to EventGrid", ex, this.TopicName, subject, this._eventPropagationPublisherOptions.TopicEndpoint);

            throw exception;
        }
    }

    private IEnumerable<EventGridEvent> GetEventsList(string subject, IEnumerable<IDomainEvent> domainEvents)
    {
        // TODO: Propagate correlation ID by setting data with "telemetryCorrelationId" property when OpenTelemetry is fully supported
        return domainEvents.Select(domainEvent => new EventGridEvent(
            subject: $"{this.TopicName}-{subject}",
            eventType: domainEvent.GetType().FullName,
            dataVersion: domainEvent.DataVersion,
            data: new BinaryData(domainEvent, SerializerOptions)));
    }
}