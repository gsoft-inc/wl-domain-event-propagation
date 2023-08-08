using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.AzureSystemEvents;
using Workleap.DomainEventPropagation.Events;

namespace Workleap.DomainEventPropagation.Extensions;

public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ISubscriptionEventGridWebhookHandler, SubscriptionEventGridWebhookHandler>();
        services.AddSingleton<IDomainEventGridWebhookHandler, DomainEventGridWebhookHandler>();
        services.AddSingleton<IAzureSystemEventGridWebhookHandler, AzureSystemEventGridWebhookHandler>();
        services.AddSingleton<IEventGridRequestHandler, EventGridRequestHandler>();

        return new EventPropagationSubscriberBuilder(services);
    }

    public static IEndpointRouteBuilder AddEventPropagationEndpoints(this IEndpointRouteBuilder builder)
    {
        builder
            .MapPost(EventsApi.Routes.DomainEvents, (
                [FromBody] object requestContent,
                HttpContext httpContext,
                IEventGridRequestHandler eventGridRequestHandler,
                CancellationToken cancellationToken) => EventsApi.HandleEventGridEvent(requestContent, httpContext, eventGridRequestHandler, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();

        builder
            .MapPost(EventsApi.Routes.SystemEvents, (
                [FromBody] object requestContent,
                HttpContext httpContext,
                IEventGridRequestHandler eventGridRequestHandler,
                CancellationToken cancellationToken) => EventsApi.HandleEventGridEvent(requestContent, httpContext, eventGridRequestHandler, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return builder;
    }
}

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;
    }

    public IServiceCollection Services { get; }
}