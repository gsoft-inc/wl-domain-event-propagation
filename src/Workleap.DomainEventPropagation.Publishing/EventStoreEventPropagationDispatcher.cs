using Azure.Messaging.EventGrid.Namespaces;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Azure.Messaging;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal class EventStoreEventPropagationDispatcher : IEventPropagationDispatcher
{
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly IEventStore _eventStore;

    public EventStoreEventPropagationDispatcher(
        IEventStore eventStore,
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._eventStore = eventStore;
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
        throw new NotSupportedException("EventGridEvent schema is not supported");
    }

    private async Task SendCloudEvents(
        DomainEventWrapperCollection domainEventWrappers,
        CancellationToken cancellationToken)
    {
        var cloudEvents = domainEventWrappers.ToCloudEvents(this._eventPropagationPublisherOptions.TopicEndpoint);
        await this._eventStore.SaveEvents(cloudEvents, cancellationToken).ConfigureAwait(false);
    }
}
