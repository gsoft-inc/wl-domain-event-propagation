using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Workleap.DomainEventPropagation.Events;

namespace Workleap.DomainEventPropagation.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder AddEventPropagationEndpoint(this IEndpointRouteBuilder builder) =>
        builder
            .MapPost(EventsApi.Routes.DomainEvents, (
                [FromBody] object requestContent,
                HttpContext httpContext,
                IEventGridRequestHandler eventGridRequestHandler,
                CancellationToken cancellationToken) => EventsApi.HandleEventGridEvent(requestContent, httpContext, eventGridRequestHandler, cancellationToken))
            .ExcludeFromDescription();

    public static RouteHandlerBuilder WithAuthorization(this RouteHandlerBuilder builder) => builder.RequireAuthorization();
}