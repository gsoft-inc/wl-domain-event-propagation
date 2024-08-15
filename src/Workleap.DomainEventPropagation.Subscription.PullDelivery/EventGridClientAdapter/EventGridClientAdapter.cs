using System.Globalization;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal sealed class EventGridClientAdapter : IEventGridClientAdapter
{
    private readonly EventGridReceiverClient _adaptee;

    public EventGridClientAdapter(EventGridReceiverClient adaptee)
    {
        this._adaptee = adaptee;
    }

    public async Task<IEnumerable<EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, int maxEvents, CancellationToken cancellationToken)
    {
        var result = await this._adaptee.ReceiveAsync(maxEvents, cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? result.Value.Details.Select(x => new EventBundle(x.Event, x.BrokerProperties.LockToken, x.BrokerProperties.DeliveryCount))
            : [];
    }

    public Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken)
    {
        return this._adaptee.AcknowledgeAsync(lockTokens, cancellationToken);
    }

    public Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, TimeSpan releaseDelay, CancellationToken cancellationToken)
    {
        return this._adaptee.ReleaseAsync(lockTokens, ((int)releaseDelay.TotalSeconds).ToString(CultureInfo.InvariantCulture), cancellationToken);
    }

    public Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken)
    {
        return this._adaptee.RejectAsync(lockTokens, cancellationToken);
    }

    internal sealed record EventBundle(CloudEvent Event, string LockToken, int DeliveryCount);
}