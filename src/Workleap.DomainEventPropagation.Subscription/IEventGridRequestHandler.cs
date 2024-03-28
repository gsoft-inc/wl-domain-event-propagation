using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal interface IEventGridRequestHandler
{
    Task<EventGridRequestResult> HandleRequestAsync(EventGridEvent[] eventGridEvents, CancellationToken cancellationToken);

    Task<EventGridRequestResult> HandleRequestAsync(CloudEvent[] cloudEvents, CancellationToken cancellationToken);
}