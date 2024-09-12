using Azure.Messaging.EventGrid.Namespaces;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Azure.Messaging;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal class EventGridEventPropagationDispatcher : IEventPropagationDispatcher
{
    private const int EventGridMaxEventsPerBatch = 1000;

    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly EventGridPublisherClient? _eventGridPublisherClient;
    private readonly EventGridSenderClient? _eventGridNamespaceClient;

    /// <summary>
    /// To support Namespace topic, we need to use the following EventGridClient https://github.com/Azure/azure-sdk-for-net/blob/Azure.Messaging.EventGrid_4.17.0-beta.1/sdk/eventgrid/Azure.Messaging.EventGridV2/src/Generated/EventGridClient.cs
    /// Note that even if this client has the same name, it is different from the deprecated one found here https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.eventgridclient?view=azure-dotnet-legacy
    /// </summary>
    public EventGridEventPropagationDispatcher(
        IAzureClientFactory<EventGridPublisherClient> eventGridPublisherClientFactory,
        IAzureClientFactory<EventGridSenderClient> eventGridClientFactory,
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;

        switch (this._eventPropagationPublisherOptions.TopicType)
        {
            case TopicType.Custom:
                this._eventGridPublisherClient = eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.EventGridClientName);
                break;
            case TopicType.Namespace:
                this._eventGridNamespaceClient = eventGridClientFactory.CreateClient(EventPropagationPublisherOptions.EventGridClientName);
                break;
            default:
                throw new InvalidOperationException($"Could not create the proper event grid client for topic type {this._eventPropagationPublisherOptions.TopicType}");
        }
    }

    public Task DispatchDomainEventsAsync(DomainEventWrapperCollection domainEventWrappers, CancellationToken cancellationToken)
    {
        return domainEventWrappers.DomainSchema switch
        {
            EventSchema.EventGridEvent => this.SendEventGridEvents(domainEventWrappers, cancellationToken),
            EventSchema.CloudEvent => this.SendCloudEvents(domainEventWrappers, cancellationToken),
            _ => throw new NotSupportedException($"Event schema {domainEventWrappers.DomainSchema} is not supported"),
        };
    }

    private async Task SendEventGridEvents(
        DomainEventWrapperCollection domainEventWrappers,
        CancellationToken cancellationToken)
    {
        if (this._eventGridPublisherClient == null)
        {
            throw new InvalidOperationException($"Unable to send eventGrid event because the {nameof(EventGridPublisherClient)} is null");
        }

        var topicType = this._eventPropagationPublisherOptions.TopicType;

        switch (topicType)
        {
            case TopicType.Custom:
                break;
            case TopicType.Namespace:
                throw new NotSupportedException("Cannot send EventGridEvents to a namespace topic");
            default:
                throw new NotSupportedException($"Topic type {topicType} is not supported");
        }

        var eventGridEvents = domainEventWrappers.ToEventGridEvents();

        foreach (var eventBatch in Chunk(eventGridEvents, EventGridMaxEventsPerBatch))
        {
            await this._eventGridPublisherClient.SendEventsAsync(eventBatch, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendCloudEvents(
        DomainEventWrapperCollection domainEventWrappers,
        CancellationToken cancellationToken)
    {
        var cloudEvents = domainEventWrappers.ToCloudEvents(this._eventPropagationPublisherOptions.TopicEndpoint);

        foreach (var eventBatch in Chunk(cloudEvents, EventGridMaxEventsPerBatch))
        {
            if (this._eventGridPublisherClient != null)
            {
                await this._eventGridPublisherClient.SendEventsAsync(eventBatch, cancellationToken).ConfigureAwait(false);
            }
            else if (this._eventGridNamespaceClient != null)
            {
                await this._eventGridNamespaceClient.SendAsync(eventBatch, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
    {
        var chunk = new List<T>(chunkSize);

        foreach (var item in source)
        {
            chunk.Add(item);
            if (chunk.Count == chunkSize)
            {
                yield return chunk;
                chunk = new List<T>(chunkSize);
            }
        }

        if (chunk.Any())
        {
            yield return chunk;
        }
    }
}
