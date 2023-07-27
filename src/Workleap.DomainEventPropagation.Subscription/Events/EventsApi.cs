using Microsoft.AspNetCore.Http;

namespace Workleap.DomainEventPropagation.Events;

internal static class EventsApi
{
    internal static class Routes
    {
        internal const string DomainEvents = "eventgrid/domainevents";

        internal const string SystemEvents = "eventgrid/systemevents";
    }

    internal static async Task<IResult> HandleEventGridEvent(
        object requestContent,
        HttpContext httpContext,
        IEventGridRequestHandler eventGridRequestHandler,
        CancellationToken cancellationToken)
    {
        var result = await eventGridRequestHandler.HandleRequestAsync(requestContent, cancellationToken);

        return result.EventGridRequestType switch
        {
            EventGridRequestType.Subscription => Results.Ok(result.Response),
            _ => Results.Ok()
        };
    }
}