using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services, Action<EventPropagationSubscriptionOptions> configure, string optionsSectionName)
    {
        this.Services = services;
        this.AddRegistrations(configure, optionsSectionName);
    }

    public IServiceCollection Services { get; }

    private void AddRegistrations(Action<EventPropagationSubscriptionOptions> configure, string optionsSectionName)
    {
        this.Services
            .AddOptions<EventPropagationSubscriptionOptions>(optionsSectionName)
            .Configure<IConfiguration>((opt, cfg) => BindFromWellKnownConfigurationSection(opt, cfg, optionsSectionName))
            .Configure(configure);

        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationSubscriptionOptions options, IConfiguration configuration, string optionsSectionName)
    {
        var section = configuration.GetSection(optionsSectionName);
        section.Bind(options);
    }
}