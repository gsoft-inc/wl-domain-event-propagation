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

    public async Task<IEnumerable<EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, int maxEvents, CancellationToken cancellationToken)
    {
        var result = await this._adaptee.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, maxEvents, cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? result.Value.Value.Select(x => new EventBundle(x.Event, x.BrokerProperties.LockToken, x.BrokerProperties.DeliveryCount))
            : [];
    }

    public Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken)
    {
        return this._adaptee.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions(lockTokens), cancellationToken);
    }

    public Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, int releaseDelayInSeconds, CancellationToken cancellationToken)
    {
        return this._adaptee.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions(lockTokens), releaseDelayInSeconds, cancellationToken);
    }

    public Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken)
    {
        return this._adaptee.RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions(lockTokens), cancellationToken);
    }

    internal sealed record EventBundle(CloudEvent Event, string LockToken, int DeliveryCount);
}