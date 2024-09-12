using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

public interface IEventStore
{
    public Task SaveEvents(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken);

    public IAsyncEnumerable<CloudEvent> ReadEvents(CancellationToken cancellationToken);

    public void RemoveEvents(IEnumerable<string> cloudEventIds, CancellationToken cancellationToken);
}