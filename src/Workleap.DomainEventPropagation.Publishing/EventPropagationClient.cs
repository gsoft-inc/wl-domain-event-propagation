using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Workleap.DomainEventPropagation.Exceptions;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
/// </summary>
internal class EventPropagationClient : IEventPropagationClient
{
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly ITelemetryClientProvider _telemetryClientProvider;

    public EventPropagationClient(
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._telemetryClientProvider = telemetryClientProvider;
    }

    public async Task PublishDomainEventAsync(string subject, IDomainEvent domainEvent)
    {
        await this.PublishDomainEventsAsync(subject, new[] { domainEvent });
    }

    public Task PublishDomainEventAsync<T>(T domainEvent) where T : IDomainEvent
        => this.PublishDomainEventAsync(typeof(T).FullName, domainEvent);

    public async Task PublishDomainEventsAsync(string subject, IEnumerable<IDomainEvent> domainEvents)
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
            await client.SendEventsAsync(GetEventsList(topicName, subject, domainEvents, this._telemetryClientProvider.GetOperationId()));

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

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents) where T : IDomainEvent
        => this.PublishDomainEventsAsync(typeof(T).FullName, domainEvents as IEnumerable<IDomainEvent>);

    private static IList<EventGridEvent> GetEventsList(string topic, string subject, IEnumerable<IDomainEvent> domainEvents, string telemetryCorrelationId = null)
    {
        var eventsList = new List<EventGridEvent>();

        foreach (var domainEvent in domainEvents)
        {
            var eventData = JsonConvert.SerializeObject(domainEvent);

            if (!string.IsNullOrEmpty(telemetryCorrelationId))
            {
                eventData = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(eventData, telemetryCorrelationId);
            }

            // Warning: the Topic field must be left as null. It is automatically set by EventGrid.
            eventsList.Add(new EventGridEvent(
                subject: $"{topic}-{subject}",
                eventType: domainEvent.GetType().FullName,
                dataVersion: domainEvent.DataVersion,
                data: BinaryData.FromString(eventData))
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTime.UtcNow
            });
        }

        return eventsList;
    }
}