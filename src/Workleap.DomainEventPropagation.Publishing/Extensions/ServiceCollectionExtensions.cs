using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationPublisherBuilder AddEventPropagationPublisher(this IServiceCollection services)
        => services.AddEventPropagationPublisher(_ => { });

    public static IEventPropagationPublisherBuilder AddEventPropagationPublisher(this IServiceCollection services, Action<EventPropagationPublisherOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.TryAddSingleton<IEventPropagationClient, EventPropagationClient>();
        services.AddEventPropagationPublisherOptions(configure);

        services.AddAzureClients(builder =>
        {
            using var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

            var topicEndpointUri = new Uri(options.TopicEndpoint);
            var topicCredentials = new AzureKeyCredential(options.TopicAccessKey);

            builder
                .AddEventGridPublisherClient(topicEndpointUri, topicCredentials)
                .WithName(EventPropagationPublisherOptions.ClientName)
                .ConfigureOptions(clientOptions =>
                {
                    clientOptions.Retry.Mode = RetryMode.Fixed;
                    clientOptions.Retry.MaxRetries = 1;
                    clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(4);
                });
        });

        return new EventPropagationPublisherBuilder(services);
    }

    internal static IServiceCollection AddEventPropagationPublisherOptions(this IServiceCollection services, Action<EventPropagationPublisherOptions> configure)
    {
        services
            .AddOptions<EventPropagationPublisherOptions>()
            .BindConfiguration(EventPropagationPublisherOptions.SectionName)
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<EventPropagationPublisherOptions>, EventPropagationPublisherOptionsValidator>();

        return services;
    }

    private sealed class EventPropagationPublisherBuilder : IEventPropagationPublisherBuilder
    {
        public EventPropagationPublisherBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public IServiceCollection Services { get; }
    }
}