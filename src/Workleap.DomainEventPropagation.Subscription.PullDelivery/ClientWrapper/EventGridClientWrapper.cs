using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.ClientWrapper;

internal class EventGridClientWrapper
{
    private readonly EventGridClient _client;

    public EventGridClientWrapper(EventGridClient client)
    {
        this._client = client;
    }

    public virtual async Task<IEnumerable<EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, int? maxEvents = null, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
    {
        var result = await this._client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, maxEvents, maxWaitTime, cancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? result.Value.Value.Select(x => new EventBundle(x.Event, x.BrokerProperties.LockToken))
            : Array.Empty<EventBundle>();
    }

    public virtual Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, AcknowledgeOptions acknowledgeOptions, CancellationToken cancellationToken = default)
    {
        return this._client.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, acknowledgeOptions, cancellationToken);
    }

    public virtual Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, ReleaseOptions releaseOptions, CancellationToken cancellationToken = default)
    {
        return this._client.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, releaseOptions, cancellationToken: cancellationToken);
    }

    public virtual Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, RejectOptions rejectOptions, CancellationToken cancellationToken = default)
    {
        return this._client.RejectCloudEventsAsync(topicName, eventSubscriptionName, rejectOptions, cancellationToken);
    }

    internal record EventBundle(CloudEvent Event, string LockToken);
}