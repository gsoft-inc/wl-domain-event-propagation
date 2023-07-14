using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionEventPropagationExtensions
{
    public static IEventPropagationPublisherBuilder AddEventPropagation(this IServiceCollection services)
    {
        services.AddSingleton<IEventPropagationClient, EventPropagationClient>();
        services.AddSingleton<ITelemetryClientProvider, TelemetryClientProvider>();

        services.AddEventPropagationPublisherOptions();

        return new EventPropagationPublisherBuilder(services);
    }

    internal static IServiceCollection AddEventPropagationPublisherOptions(this IServiceCollection services)
    {
        services
            .AddOptions<EventPropagationPublisherOptions>()
            .BindConfiguration(EventPropagationPublisherOptions.SectionName)
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