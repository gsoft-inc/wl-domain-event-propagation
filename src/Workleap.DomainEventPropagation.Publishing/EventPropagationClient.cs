using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
/// </summary>
internal sealed class EventPropagationClient<TDispatcher> : IResilientEventPropagationClient
    where TDispatcher : IEventPropagationDispatcher
{
    private readonly EventPropagationPublisherOptions _eventPropagationPublisherOptions;
    private readonly DomainEventsHandlerDelegate _pipeline;

    public EventPropagationClient(
        IOptions<EventPropagationPublisherOptions> eventPropagationPublisherOptions,
        TDispatcher eventPropagationDispatcher,
        IEnumerable<IPublishingDomainEventBehavior> additionalPublishingDomainEventBehaviors)
    {
        this._eventPropagationPublisherOptions = eventPropagationPublisherOptions.Value;
        this._pipeline = additionalPublishingDomainEventBehaviors.Reverse().Aggregate((DomainEventsHandlerDelegate)eventPropagationDispatcher.DispatchDomainEventsAsync, BuildPipeline);
    }

    private static DomainEventsHandlerDelegate BuildPipeline(DomainEventsHandlerDelegate accumulator, IPublishingDomainEventBehavior next)
    {
        return (events, cancellationToken) => next.HandleAsync(events, accumulator, cancellationToken);
    }

    public Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.InternalPublishDomainEventsAsync(new[] { domainEvent }, null, cancellationToken);

    public Task PublishDomainEventAsync<T>(T domainEvent, Action<IDomainEventMetadata> configureDomainEventMetadata, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.InternalPublishDomainEventsAsync(new[] { domainEvent }, configureDomainEventMetadata, cancellationToken);

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.InternalPublishDomainEventsAsync(domainEvents, null, cancellationToken);

    public Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, Action<IDomainEventMetadata> configureDomainEventMetadata, CancellationToken cancellationToken)
        where T : IDomainEvent
        => this.InternalPublishDomainEventsAsync(domainEvents, configureDomainEventMetadata, cancellationToken);

    private async Task InternalPublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, Action<IDomainEventMetadata>? configureDomainEventMetadata, CancellationToken cancellationToken)
        where T : IDomainEvent
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var domainEventWrappers = DomainEventWrapperCollection.Create(domainEvents, configureDomainEventMetadata);
        if (domainEventWrappers.Count == 0)
        {
            return;
        }

        if (configureDomainEventMetadata != null && domainEventWrappers.DomainSchema != EventSchema.CloudEvent)
        {
            throw new NotSupportedException("Domain event configuration is only supported for CloudEvents");
        }

        try
        {
            await this._pipeline(domainEventWrappers, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new EventPropagationPublishingException(domainEventWrappers.DomainEventName, this._eventPropagationPublisherOptions.TopicEndpoint, ex);
        }
    }
}

