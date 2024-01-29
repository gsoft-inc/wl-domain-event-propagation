using Azure.Messaging;

namespace Workleap.DomainEventPropagation;

internal interface ISubscriptionHandler
{
    Task<EventProcessingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}