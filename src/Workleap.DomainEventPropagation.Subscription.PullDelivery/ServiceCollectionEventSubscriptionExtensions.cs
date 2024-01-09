using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

public static class ServiceCollectionEventSubscriptionExtensions
{
    public static IEventPropagationSubscriberBuilder AddPullDeliverySubscription(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return new EventPropagationSubscriberBuilder(services);
    }

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder)
        => builder.AddSubscriber(_ => { });

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder, string optionsSectionName)
        => builder.AddSubscriber(_ => { }, optionsSectionName);

    public static IEventPropagationSubscriberBuilder AddSubscriber(this IEventPropagationSubscriberBuilder builder, Action<EventPropagationSubscriptionOptions> configure, string optionsSectionName = EventPropagationSubscriptionOptions.DefaultSectionName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.Services.AddOptions<EventPropagationSubscriptionOptions>(optionsSectionName)
            .Configure<IConfiguration>((opt, cfg) => BindFromWellKnownConfigurationSection(opt, cfg, optionsSectionName))
            .Configure(configure);

        builder.Services.AddScoped<EventGridClientDescriptor>(sp => new EventGridClientDescriptor(optionsSectionName));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());

        builder.Services.AddAzureClients(builder =>
        {
            builder.AddClient<EventGridClient, EventGridClientOptions>((opts, sp) => EventGridClientFactory(opts, sp, optionsSectionName))
                .WithName(optionsSectionName)
                .ConfigureOptions(ConfigureEventGridClientOptions);
        });

        return builder;
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationSubscriptionOptions options, IConfiguration configuration, string optionsSectionName)
    {
        var section = configuration.GetSection(optionsSectionName);
        section.Bind(options);
    }

    private static EventGridClient EventGridClientFactory(EventGridClientOptions eventGridClientOptions, IServiceProvider serviceProvider, string optionsSectionName)
    {
        var subscriberOptions = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>().Get(optionsSectionName);
        var topicEndpointUri = new Uri(subscriberOptions.TopicEndpoint);

        return subscriberOptions.TokenCredential is not null
            ? new EventGridClient(topicEndpointUri, subscriberOptions.TokenCredential, eventGridClientOptions)
            : new EventGridClient(topicEndpointUri, new AzureKeyCredential(subscriberOptions.TopicAccessKey), eventGridClientOptions);
    }

    private static void ConfigureEventGridClientOptions(EventGridClientOptions options)
    {
        options.Retry.Mode = RetryMode.Fixed;
        options.Retry.MaxRetries = 20;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(4);
    }
}