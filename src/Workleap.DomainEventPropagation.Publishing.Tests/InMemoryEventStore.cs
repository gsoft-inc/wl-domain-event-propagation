using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;
internal class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, CloudEvent> _cloudEvents = new();

    public async IAsyncEnumerable<CloudEvent> ReadEvents([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var value in this._cloudEvents.Values)
        {
            await Task.Yield();
            yield return value;
        }
    }

    public void RemoveEvents(IEnumerable<string> cloudEventIds, CancellationToken cancellationToken)
    {
        foreach (var cloudEventId in cloudEventIds)
        {
            this._cloudEvents.TryRemove(cloudEventId, out _);
        }
    }

    public Task SaveEvents(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken)
    {
        foreach (var cloudEvent in cloudEvents)
        {
            this._cloudEvents.TryAdd(cloudEvent.Id, cloudEvent);
        }

        return Task.CompletedTask;
    }
}
