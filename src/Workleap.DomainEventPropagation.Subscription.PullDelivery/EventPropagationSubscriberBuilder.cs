using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationSubscriberBuilder : IEventPropagationSubscriberBuilder
{
    public EventPropagationSubscriberBuilder(IServiceCollection services, Action<EventPropagationSubscriptionOptions> configure)
    {
        this.Services = services;
        this.AddRegistrations(configure);
    }

    public IServiceCollection Services { get; }

    private void AddRegistrations(Action<EventPropagationSubscriptionOptions> configure)
    {
        this.Services
            .AddOptions<EventPropagationSubscriptionOptions>()
            .Configure<IConfiguration>(BindFromWellKnownConfigurationSection)
            .Configure(configure);

        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationSubscriptionOptions options, IConfiguration configuration)
    {
        configuration.GetSection(EventPropagationSubscriptionOptions.SectionName).Bind(options);
    }
}