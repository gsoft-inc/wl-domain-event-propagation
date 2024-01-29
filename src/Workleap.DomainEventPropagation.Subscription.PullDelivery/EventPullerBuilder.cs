using System.Reflection;
using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

public interface IEventPullerBuilder
{
    IServiceCollection Services { get; }

    IEventPullerBuilder AddSubscriber(Action<ISubscriptionHandlerBuilder> configureSubscriber);

    IEventPullerBuilder AddSubscriber(
        string optionsSectionName,
        Action<ISubscriptionHandlerBuilder> configureSubscriber);

    IEventPullerBuilder AddSubscriber(
        string optionsSectionName,
        Action<EventPropagationSubscriptionOptions> configureOptions,
        Action<ISubscriptionHandlerBuilder> configureSubscriber);

    IEventPullerBuilder AddGlobalDomainEventHandlers(Assembly assembly);
}

internal sealed class EventPullerBuilder : IEventPullerBuilder
{
    private readonly IGlobalDomainEventTypeRegistry _globalDomainEventTypeRegistry;

    public EventPullerBuilder(IServiceCollection services)
    {
        this.Services = services;

        this._globalDomainEventTypeRegistry = services.Where(x => x.ServiceType == typeof(IGlobalDomainEventTypeRegistry))
            .Select(x => x.ImplementationInstance)
            .OfType<DomainEventTypeRegistry>()
            .FirstOrDefault() ?? new DomainEventTypeRegistry();
        this.Services.AddSingleton(this._globalDomainEventTypeRegistry);

        this.Services.AddTransient<IEventGridClientWrapperFactory, EventGridClientAdapterFactory>();
        this.Services.AddHostedService<EventPuller>();
    }

    public IServiceCollection Services { get; }

    public IEventPullerBuilder AddSubscriber(Action<ISubscriptionHandlerBuilder> configureSubscriber)
        => this.AddSubscriber(EventPropagationSubscriptionOptions.DefaultSectionName, _ => { }, configureSubscriber);

    public IEventPullerBuilder AddSubscriber(string optionsSectionName, Action<ISubscriptionHandlerBuilder> configureSubscriber)
        => this.AddSubscriber(optionsSectionName, _ => { }, configureSubscriber);

    public IEventPullerBuilder AddSubscriber(
        string optionsSectionName,
        Action<EventPropagationSubscriptionOptions> configureOptions,
        Action<ISubscriptionHandlerBuilder> configureSubscriber)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());
        this.Services.AddOptions<EventPropagationSubscriptionOptions>(optionsSectionName)
            .Configure<IConfiguration>((opt, cfg) => BindFromWellKnownConfigurationSection(opt, cfg, optionsSectionName))
            .Configure(configureOptions);

        this.Services.AddTransient<EventGridClientDescriptor>(sp => new EventGridClientDescriptor(optionsSectionName));
        this.Services.AddAzureClients(builder =>
        {
            builder.AddClient<EventGridClient, EventGridClientOptions>((opts, sp) => EventGridClientFactory(opts, sp, optionsSectionName))
                .WithName(optionsSectionName)
                .ConfigureOptions(ConfigureEventGridClientOptions);
        });
        var subscriptionHandlerBuilder = new SubscriptionHandlerBuilder(this.Services, optionsSectionName);
        configureSubscriber(subscriptionHandlerBuilder);

        return this;
    }

    public IEventPullerBuilder AddGlobalDomainEventHandlers(Assembly assembly)
    {
        this.Services.AddDomainEventHandlers(this._globalDomainEventTypeRegistry, assembly, (type) => type.GetCustomAttribute<GlobalSubscriptionHandlerAttribute>() != null);
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

    private static void ConfigureEventGridClientOptions(EventGridClientOptions options)
    {
        // options.Retry.Mode = RetryMode.Fixed;
        // options.Retry.MaxRetries = 20;
        options.Diagnostics.IsLoggingEnabled = false;
    }
}