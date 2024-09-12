using System.Collections;
using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventWrapperCollection : IReadOnlyCollection<DomainEventWrapper>
{
    private readonly DomainEventWrapper[] _domainEventWrappers;

    private DomainEventWrapperCollection(IEnumerable<DomainEventWrapper> domainEventWrappers, Action<IDomainEventMetadata>? configureDomainEventMetadata, string domainEventName, EventSchema schema)
    {
        this._domainEventWrappers = domainEventWrappers.ToArray();
        this.DomainEventName = domainEventName;
        this.DomainSchema = schema;
        this.ConfigureDomainEventMetadata = configureDomainEventMetadata;
    }

    public int Count => this._domainEventWrappers.Length;

    public string DomainEventName { get; }

    public EventSchema DomainSchema { get; }

    public Action<IDomainEventMetadata>? ConfigureDomainEventMetadata { get; }

    public static DomainEventWrapperCollection Create<T>(IEnumerable<T> domainEvents, Action<IDomainEventMetadata>? configureDomainEventMetadata)
        where T : IDomainEvent
    {
        var domainEventWrappers = domainEvents.Select(DomainEventWrapper.Wrap).ToArray();

        return new DomainEventWrapperCollection(domainEventWrappers, configureDomainEventMetadata, DomainEventNameCache.GetName<T>(), DomainEventSchemaCache.GetEventSchema<T>());
    }

    public IEnumerator<DomainEventWrapper> GetEnumerator()
    {
        // See https://stackoverflow.com/questions/1272673/obtain-generic-enumerator-from-an-array
        return ((IEnumerable<DomainEventWrapper>)this._domainEventWrappers).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerable<EventGridEvent> ToEventGridEvents()
    {
        return this._domainEventWrappers.Select(wrapper => wrapper.ToEventGridEvent());
    }

    public IEnumerable<CloudEvent> ToCloudEvents(string topicEndpoint)
    {
        var cloudEvents = this._domainEventWrappers.Select(wrapper =>
        {
            var cloudEvent = wrapper.ToCloudEvent(topicEndpoint);

            this.ConfigureDomainEventMetadata?.Invoke(new DomainEventMetadataWrapper(cloudEvent));

            return cloudEvent;
        });

        return cloudEvents;
    }
}