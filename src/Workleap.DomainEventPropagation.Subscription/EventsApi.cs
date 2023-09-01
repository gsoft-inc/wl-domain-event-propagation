using System.ComponentModel.DataAnnotations;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Workleap.DomainEventPropagation;

internal static class EventsApi
{
    internal static async Task<IResult> HandleEventGridEvents(
        [FromBody, Required] EventGridEvent[] eventGridEvents,
        [FromServices] IEventGridRequestHandler eventGridRequestHandler,
        CancellationToken cancellationToken)
    {
        var result = await eventGridRequestHandler.HandleRequestAsync(eventGridEvents, cancellationToken).ConfigureAwait(false);

        return result.RequestType switch
        {
            EventGridRequestType.Subscription => Results.Ok(result.ValidationResponse),
            _ => Results.Ok(),
        };
    }

    internal static class Routes
    {
        internal const string DomainEvents = "eventgrid/domainevents";
    }
}