using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

public static class ServiceCollectionEventSubscriptionExtensions
{
    public static IEventPropagationSubscriberBuilder AddPullDeliverySubscription(this IServiceCollection services)
    {
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

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventPropagationSubscriptionOptions>, EventPropagationSubscriptionOptionsValidator>());

        return builder;
    }

    private static void BindFromWellKnownConfigurationSection(EventPropagationSubscriptionOptions options, IConfiguration configuration, string optionsSectionName)
    {
        var section = configuration.GetSection(optionsSectionName);
        section.Bind(options);
    }
}