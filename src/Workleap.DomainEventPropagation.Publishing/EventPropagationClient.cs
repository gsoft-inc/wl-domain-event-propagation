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
    private const string DomainEventDefaultVersion = "1.0";

    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly IPublishingDomainEventBehavior[] _publishingDomainEventBehaviors;

    public EventPropagationClient(
        IAzureClientFactory<EventGridPublisherClient> eventGridPublisherClientFactory,
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions,
        IEnumerable<IPublishingDomainEventBehavior> publishingDomainEventBehaviors)
    {
        this._eventGridPublisherClient = eventGridPublisherClientFactory.CreateClient(EventPropagationPublisherOptions.ClientName);
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._publishingDomainEventBehaviors = publishingDomainEventBehaviors.Reverse().ToArray();
    }

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
            async Task Handler(IEnumerable<DomainEventWrapper> events)
            {
                var eventGridEvents = events.Select(domainEvent => new EventGridEvent(
                    subject: $"{this._eventPropagationPublisherOptions.TopicName}-{DomainEventNameCache.GetName<T>()}",
                    eventType: domainEvent.DomainEventName,
                    dataVersion: DomainEventDefaultVersion,
                    data: new BinaryData(domainEvent.RawJson)));

                await this._eventGridPublisherClient.SendEventsAsync(eventGridEvents, cancellationToken).ConfigureAwait(false);
            }

            var pipeline = this._publishingDomainEventBehaviors.Aggregate(Handler, (DomainEventsHandlerDelegate accumulator, IPublishingDomainEventBehavior next) =>
            {
                return events => next.Handle(events, accumulator, cancellationToken);
            });

            var domainEventWrappers = domainEvents.Select(DomainEventWrapper.Wrap);
            await pipeline(domainEventWrappers).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new EventPropagationPublishingException(DomainEventNameCache.GetName<T>(), this._eventPropagationPublisherOptions.TopicName, this._eventPropagationPublisherOptions.TopicEndpoint, ex);
        }
    }
}