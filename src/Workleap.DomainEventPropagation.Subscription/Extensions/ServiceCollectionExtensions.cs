using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

using Workleap.EventPropagation.Subscription.AzureSystemEvents;
using Workleap.EventPropagation.Telemetry;

namespace Workleap.EventPropagation.Subscription.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services, Action<EventPropagationSubscriberOptions> configureOptions = null)
    {
        services.AddSingleton<ISubscriptionEventGridWebhookHandler, SubscriptionEventGridWebhookHandler>();
        services.AddSingleton<IDomainEventGridWebhookHandler, DomainEventGridWebhookHandler>();
        services.AddSingleton<IAzureSystemEventGridWebhookHandler, AzureSystemEventGridWebhookHandler>();
        services.AddSingleton<ISubscriptionTopicValidator, SubscriptionTopicValidator>();
        services.AddSingleton<ITelemetryClientProvider, TelemetryClientProvider>();
        services.AddSingleton<IEventGridRequestHandler, EventGridRequestHandler>();

        services.AddOptions<EventPropagationSubscriberOptions>()
            .Configure(configureOptions ?? (_ => {}))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return new EventPropagationSubscriberBuilder(services);
    }

    private sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
    {
        public EventPropagationSubscriberBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public IServiceCollection Services { get; }
    }
}