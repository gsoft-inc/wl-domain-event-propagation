using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal sealed class EventGridClientAdapter : IEventGridClientAdapter
{
    private readonly EventGridClient _client;

    public EventGridClientAdapter(EventGridClient client)
    {
        this._client = client;
    }

    public async Task<IEnumerable<EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, CancellationToken cancellationToken)
    {
        var result = await this._client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? result.Value.Value.Select(x => new EventBundle(x.Event, x.BrokerProperties.LockToken))
            : Array.Empty<EventBundle>();
    }

    public Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken)
    {
        return this._client.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions(new[] { lockToken }), cancellationToken);
    }

    public Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken)
    {
        return this._client.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions(new[] { lockToken }), cancellationToken: cancellationToken);
    }

    public Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken)
    {
        return this._client.RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions(new[] { lockToken }), cancellationToken);
    }

    internal sealed record EventBundle(CloudEvent Event, string LockToken);
}