using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services)
        => services.AddEventPropagationSubscriber(_ => { });

    public static IEventPropagationSubscriberBuilder AddEventPropagationSubscriber(this IServiceCollection services, Action<EventPropagationSubscriberOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.AddSingleton<ISubscriptionEventGridWebhookHandler, SubscriptionEventGridWebhookHandler>();
        services.AddSingleton<IDomainEventGridWebhookHandler, DomainEventGridWebhookHandler>();
        services.AddSingleton<IAzureSystemEventGridWebhookHandler, AzureSystemEventGridWebhookHandler>();
        services.AddSingleton<ISubscriptionTopicValidator, SubscriptionTopicValidator>();
        services.AddSingleton<ITelemetryClientProvider, TelemetryClientProvider>();
        services.AddSingleton<IEventGridRequestHandler, EventGridRequestHandler>();

        services
            .AddOptions<EventPropagationSubscriberOptions>()
            .BindConfiguration(EventPropagationSubscriberOptions.SectionName)
            .PostConfigure(configure)
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