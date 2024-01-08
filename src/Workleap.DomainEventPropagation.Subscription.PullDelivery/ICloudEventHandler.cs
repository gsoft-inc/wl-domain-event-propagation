using Azure.Messaging;

namespace Workleap.DomainEventPropagation;

internal enum EventProcessingStatus
{
    Handled,
    Released,
    Rejected,
}

internal interface ICloudEventHandler
{
    Task<EventProcessingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}