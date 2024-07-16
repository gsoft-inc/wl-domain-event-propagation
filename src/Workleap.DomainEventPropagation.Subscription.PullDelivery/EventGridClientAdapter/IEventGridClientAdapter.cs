namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal interface IEventGridClientAdapter
{
    Task<IEnumerable<EventGridClientAdapter.EventBundle>> ReceiveCloudEventsAsync(string topicName, string eventSubscriptionName, int maxEvents, CancellationToken cancellationToken);

    Task AcknowledgeCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken);

    Task ReleaseCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, int releaseDelayInSeconds, CancellationToken cancellationToken);

    Task RejectCloudEventsAsync(string topicName, string eventSubscriptionName, IEnumerable<string> lockTokens, CancellationToken cancellationToken);
}