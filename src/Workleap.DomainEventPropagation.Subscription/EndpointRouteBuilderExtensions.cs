using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Workleap.DomainEventPropagation;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapEventPropagationEndpoint(this IEndpointRouteBuilder builder)
        => MapEventPropagationEndpoint(builder, EventsApi.Routes.DomainEvents);

    public static RouteHandlerBuilder MapEventPropagationEndpoint(this IEndpointRouteBuilder builder, string pattern) => builder
        .MapPost(pattern, EventsApi.HandleEventGridEvents)
        .ExcludeFromDescription();
}