using Azure.Messaging;

namespace Workleap.DomainEventPropagation;

internal enum HandlingStatus
{
    Handled,
    Released,
    Rejected,
}

internal interface ICloudEventHandler
{
    Task<HandlingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}