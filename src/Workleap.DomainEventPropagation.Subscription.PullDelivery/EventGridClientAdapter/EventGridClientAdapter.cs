using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal sealed class EventGridClientAdapter : IEventGridClientAdapter
{
    private readonly EventGridClient _adaptee;

    public EventGridClientAdapter(EventGridClient adaptee)
    {
        this._adaptee = adaptee;
    }

    public async Task<IEnumerable<EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, CancellationToken cancellationToken)
    {
        var result = await this._adaptee.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? result.Value.Value.Select(x => new EventBundle(x.Event, x.BrokerProperties.LockToken))
            : Array.Empty<EventBundle>();
    }

    public Task AcknowledgeCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken)
    {
        return this._adaptee.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions(new[] { lockToken }), cancellationToken);
    }

    public Task ReleaseCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken)
    {
        return this._adaptee.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions(new[] { lockToken }), cancellationToken: cancellationToken);
    }

    public Task RejectCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken)
    {
        return this._adaptee.RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions(new[] { lockToken }), cancellationToken);
    }

    internal sealed record EventBundle(CloudEvent Event, string LockToken);
}