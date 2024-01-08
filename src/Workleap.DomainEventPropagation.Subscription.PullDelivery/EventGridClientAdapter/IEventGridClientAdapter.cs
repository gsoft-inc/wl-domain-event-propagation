using Azure.Messaging.EventGrid.Namespaces;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal interface IEventGridClientAdapter
{
    Task<IEnumerable<EventGridClientAdapter.EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, CancellationToken cancellationToken);

    Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken);

    Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken);

    Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, string? lockToken, CancellationToken cancellationToken);
}