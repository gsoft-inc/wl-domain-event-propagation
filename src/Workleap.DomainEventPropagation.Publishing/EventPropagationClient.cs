using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
/// </summary>
internal class EventPropagationClient : IEventPropagationClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly ITelemetryClientProvider _telemetryClientProvider;

    public EventPropagationClient(
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._telemetryClientProvider = telemetryClientProvider;
    }

    public async Task PublishDomainEventAsync(string subject, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await this.PublishDomainEventsAsync(subject, new[] { domainEvent }, cancellationToken);
    }

    public Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent
        => this.PublishDomainEventAsync(typeof(T).FullName, domainEvent, cancellationToken);

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken) where T : IDomainEvent
        => this.PublishDomainEventsAsync(typeof(T).FullName, domainEvents as IEnumerable<IDomainEvent>, cancellationToken);

    public async Task PublishDomainEventsAsync(string subject, IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        var topicName = this._eventPropagationPublisherOptions.TopicName;
        var topicEndpoint = this._eventPropagationPublisherOptions.TopicEndpoint;
        var topicAccessKey = this._eventPropagationPublisherOptions.TopicAccessKey;

        var topicEndpointUri = new Uri(topicEndpoint);
        var topicCredentials = new AzureKeyCredential(topicAccessKey);
        var client = new EventGridPublisherClient(
            topicEndpointUri,
            topicCredentials,
            new EventGridPublisherClientOptions
            {
                Retry = { Mode = RetryMode.Fixed, MaxRetries = 1, NetworkTimeout = TimeSpan.FromSeconds(4) }
            });

        try
        {
            var eventGridEvents = GetEventsList(topicName, subject, domainEvents);

            await client.SendEventsAsync(eventGridEvents, cancellationToken);

            this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventsPropagated, $"Propagated domain event with subject '{subject}' on topic '{topicName}'", TelemetryHelper.GetDomainEventTypes(domainEvents));
        }
        catch (Exception ex)
        {
            var exception = new EventPropagationPublishingException("An error occured while publishing events to EventGrid", ex)
            {
                TopicName = topicName,
                Subject = subject,
                TopicEndpoint = topicEndpoint
            };

            this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventsPropagationFailed, $"Domain event propagation failed with subject '{subject}' on topic '{topicName}'", TelemetryHelper.GetDomainEventTypes(domainEvents));
            this._telemetryClientProvider.TrackException(exception);

            throw exception;
        }
    }

    private static IEnumerable<EventGridEvent> GetEventsList(string topic, string subject, IEnumerable<IDomainEvent> domainEvents)
    {
        // TODO: Propagate correlation ID by setting data with "telemetryCorrelationId" property when OpenTelemetry is fully supported
        return domainEvents.Select(domainEvent => new EventGridEvent(
            subject: $"{topic}-{subject}",
            eventType: domainEvent.GetType().FullName,
            dataVersion: domainEvent.DataVersion,
            data: new BinaryData(domainEvent, SerializerOptions)));
    }
}