using Microsoft.ApplicationInsights.DataContracts;

namespace Workleap.DomainEventPropagation;

internal interface IEventGridRequestHandler
{
    Task<EventGridRequestResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken, RequestTelemetry requestTelemetry = default);
}