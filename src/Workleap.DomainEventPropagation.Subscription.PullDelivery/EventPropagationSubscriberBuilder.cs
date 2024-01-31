using Azure;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services)
    {
        this.Services = services;

        this.Services.AddSingleton<IDomainEventTypeRegistry, DomainEventTypeRegistry>();
        this.Services.AddTransient<IEventGridClientWrapperFactory, EventGridClientAdapterFactory>();
        this.Services.AddTransient<ICloudEventHandler, CloudEventHandler>();
        this.Services.AddHostedService<EventPuller>();
    }

    public IServiceCollection Services { get; }

    public IEventPropagationSubscriberBuilder AddTopicSubscription()
        => this.AddTopicSubscription(EventPropagationSubscriptionOptions.DefaultSectionName);

    public IEventPropagationSubscriberBuilder AddTopicSubscription(string optionsSectionName)
        => this.AddTopicSubscription(optionsSectionName, _ => { });

    public IEventPropagationSubscriberBuilder AddTopicSubscription(
        string optionsSectionName,
        Action<EventPropagationSubscriptionOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        this.Services.AddOptions<EventPropagationSubscriptionOptions>(optionsSectionName)
            .Configure<IConfiguration>((opt, cfg) => BindFromWellKnownConfigurationSection(opt, cfg, optionsSectionName))
            .Configure(configureOptions);

        this.Services.AddTransient<EventGridClientDescriptor>(sp => new EventGridClientDescriptor(optionsSectionName));
        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());

        this.Services.AddAzureClients(builder =>
        {
            builder.AddClient<EventGridClient, EventGridClientOptions>((opts, sp) => EventGridClientFactory(opts, sp, optionsSectionName))
                .WithName(optionsSectionName);
        });

        return this;
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
}