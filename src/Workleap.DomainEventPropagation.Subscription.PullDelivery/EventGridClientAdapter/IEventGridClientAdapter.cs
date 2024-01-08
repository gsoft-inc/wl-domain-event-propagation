using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal interface IEventGridClientAdapter
{
    Task<IEnumerable<EventGridClientAdapter.EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, CancellationToken cancellationToken);

    Task AcknowledgeCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken);

    Task ReleaseCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken);

    Task RejectCloudEventAsync(string topicName, string eventSubscriptionName, string lockToken, CancellationToken cancellationToken);
}