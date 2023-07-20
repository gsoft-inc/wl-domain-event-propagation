using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Workleap.DomainEventPropagation.Events;

internal static class EventsApi
{
    internal static class Routes
    {
        internal const string DomainEvents = "eventgrid/domainevents";

        internal const string SystemEvents = "eventgrid/systemevents";
    }

    internal static async Task<IResult> HandleEventGridEvent(
        [FromBody] object requestContent,
        HttpContext httpContext,
        IEventGridRequestHandler eventGridRequestHandler,
        CancellationToken cancellationToken)
    {
        var requestTelemetry = httpContext.Features.Get<RequestTelemetry>();

        var result = await eventGridRequestHandler.HandleRequestAsync(requestContent, cancellationToken, requestTelemetry: requestTelemetry);

        return result.EventGridRequestType switch
        {
            EventGridRequestType.Subscription => Results.Ok(result.Response),
            _ => Results.Ok()
        };
    }
}