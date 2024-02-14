using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationPublisherBuilder : IEventPropagationPublisherBuilder
{
    public EventPropagationPublisherBuilder(IServiceCollection services, Action<EventPropagationPublisherOptions> configure)
    {
        this.Services = services;
        this.AddRegistrations(configure);
    }

    public IServiceCollection Services { get; }

    private void AddRegistrations(Action<EventPropagationPublisherOptions> configure)
    {
        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationPublisherOptions>, EventPropagationPublisherOptionsValidator>());

        this.Services
            .AddOptions<EventPropagationPublisherOptions>()
            .Configure<IConfiguration>(BindFromWellKnownConfigurationSection)
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        this.Services.TryAddSingleton<IEventPropagationClient, EventPropagationClient>();
        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPublishingDomainEventBehavior, TracingPublishingDomainEventBehavior>());

        this.Services.AddAzureClients(ConfigureEventPublisher);
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationPublisherOptions options, IConfiguration configuration)
    {
        configuration.GetSection(EventPropagationPublisherOptions.SectionName).Bind(options);
    }

    private static void ConfigureEventPublisher(AzureClientFactoryBuilder builder)
    {
        builder.AddClient<EventGridPublisherClient, EventGridPublisherClientOptions>(EventGridPublisherClientFactory)
            .WithName(EventPropagationPublisherOptions.CustomTopicClientName)
            .ConfigureOptions(ConfigureEventGridPublisherClientOptions);

        builder.AddClient<EventGridClient, EventGridClientOptions>(EventGridClientFactory)
            .WithName(EventPropagationPublisherOptions.NamespaceTopicClientName)
            .ConfigureOptions(ConfigureEventGridClientOptions);
    }

    private static EventGridPublisherClient EventGridPublisherClientFactory(EventGridPublisherClientOptions clientOptions, IServiceProvider serviceProvider)
    {
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;
        var topicEndpointUri = new Uri(publisherOptions.TopicEndpoint);

        return publisherOptions.TokenCredential is not null
            ? new EventGridPublisherClient(topicEndpointUri, publisherOptions.TokenCredential, clientOptions)
            : new EventGridPublisherClient(topicEndpointUri, new AzureKeyCredential(publisherOptions.TopicAccessKey), clientOptions);
    }

    private static void ConfigureEventGridPublisherClientOptions(EventGridPublisherClientOptions options)
    {
        options.Retry.Mode = RetryMode.Fixed;
        options.Retry.MaxRetries = 1;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(4);
    }

    private static EventGridClient EventGridClientFactory(EventGridClientOptions clientOptions, IServiceProvider serviceProvider)
    {
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;
        var topicEndpointUri = new Uri(publisherOptions.TopicEndpoint);

        return publisherOptions.TokenCredential is not null
            ? new EventGridClient(topicEndpointUri, publisherOptions.TokenCredential, clientOptions)
            : new EventGridClient(topicEndpointUri, new AzureKeyCredential(publisherOptions.TopicAccessKey), clientOptions);
    }

    private static void ConfigureEventGridClientOptions(EventGridClientOptions options)
    {
        options.Retry.Mode = RetryMode.Fixed;
        options.Retry.MaxRetries = 1;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(4);
    }
}