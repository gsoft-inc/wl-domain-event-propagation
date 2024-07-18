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
        this.Services
            .AddOptions<EventPropagationPublisherOptions>()
            .Configure<IConfiguration>(BindFromWellKnownConfigurationSection)
            .Configure(configure);

        this.Services.TryAddSingleton<IEventPropagationClient, EventPropagationClient>();
        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationPublisherOptions>, EventPropagationPublisherOptionsValidator>());
        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPublishingDomainEventBehavior, TracingPublishingDomainEventBehavior>());

        this.Services.AddAzureClients(ConfigureEventPublisherClients);
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationPublisherOptions options, IConfiguration configuration)
    {
        configuration.GetSection(EventPropagationPublisherOptions.SectionName).Bind(options);
    }

    private static void ConfigureEventPublisherClients(AzureClientFactoryBuilder builder)
    {
        builder.AddClient<EventGridPublisherClient, EventGridPublisherClientOptions>(EventGridPublisherClientFactory)
            .WithName(EventPropagationPublisherOptions.EventGridClientName)
            .ConfigureOptions(ConfigureClientOptions);

        builder.AddClient<EventGridClient, EventGridClientOptions>(EventGridClientFactory)
            .WithName(EventPropagationPublisherOptions.EventGridClientName)
            .ConfigureOptions(ConfigureClientOptions);
    }

    private static EventGridPublisherClient EventGridPublisherClientFactory(EventGridPublisherClientOptions clientOptions, IServiceProvider serviceProvider)
    {
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;
        var topicEndpointUri = new Uri(publisherOptions.TopicEndpoint);

        return publisherOptions.TokenCredential is not null
            ? new EventGridPublisherClient(topicEndpointUri, publisherOptions.TokenCredential, clientOptions)
            : new EventGridPublisherClient(topicEndpointUri, new AzureKeyCredential(publisherOptions.TopicAccessKey), clientOptions);
    }

    private static EventGridClient EventGridClientFactory(EventGridClientOptions clientOptions, IServiceProvider serviceProvider)
    {
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;
        var topicEndpointUri = new Uri(publisherOptions.TopicEndpoint);

        return publisherOptions.TokenCredential is not null
            ? new EventGridClient(topicEndpointUri, publisherOptions.TokenCredential, clientOptions)
            : new EventGridClient(topicEndpointUri, new AzureKeyCredential(publisherOptions.TopicAccessKey), clientOptions);
    }

    private static void ConfigureClientOptions(ClientOptions options)
    {
        options.Retry.Mode = RetryMode.Fixed;
        options.Retry.MaxRetries = 1;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(4);
    }
}