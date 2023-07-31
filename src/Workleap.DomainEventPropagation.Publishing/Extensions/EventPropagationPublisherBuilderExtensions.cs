using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Extensions;

[ExcludeFromCodeCoverage]
public static class EventPropagationPublisherBuilderExtensions
{
    public static IEventPropagationPublisherBuilder AddTopicProvider<T>(this IEventPropagationPublisherBuilder builder) where T : class, ITopicProvider
    {
        builder.Services.AddSingleton<ITopicProvider, T>();

        return builder;
    }
}