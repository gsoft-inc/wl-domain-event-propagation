using System.Collections;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventWrapperCollection : IReadOnlyCollection<DomainEventWrapper>
{
    private readonly DomainEventWrapper[] _domainEventWrappers;

    private DomainEventWrapperCollection(IEnumerable<DomainEventWrapper> domainEventWrappers, string domainEventName, EventSchema schema)
    {
        this._domainEventWrappers = domainEventWrappers.ToArray();
        this.DomainEventName = domainEventName;
        this.DomainSchema = schema;
    }

    public int Count => this._domainEventWrappers.Length;

    public string DomainEventName { get; }

    public EventSchema DomainSchema { get; }

    public static DomainEventWrapperCollection Create<T>(IEnumerable<T> domainEvents)
        where T : IDomainEvent
    {
        var domainEventWrappers = domainEvents.Select(DomainEventWrapper.Wrap).ToArray();
        
        return new DomainEventWrapperCollection(domainEventWrappers, DomainEventNameCache.GetName<T>(), DomainEventSchemaCache.GetEventSchema<T>());
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
}