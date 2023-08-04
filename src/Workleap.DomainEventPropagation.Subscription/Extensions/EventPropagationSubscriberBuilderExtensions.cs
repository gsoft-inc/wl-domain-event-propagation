using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static System.FormattableString;

namespace Workleap.DomainEventPropagation.Extensions;

[ExcludeFromCodeCoverage]
public static class EventPropagationSubscriberBuilderExtensions
{
    private static readonly Type DomainEventHandlerOpenGenericInterface = typeof(IDomainEventHandler<>);

    /// <summary>
    /// Adds the given handler to the DI container if it implements <see cref="IAzureSystemEventHandler{TDomainEvent}"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEventPropagationSubscriberBuilder AddTopicProvider<T>(this IEventPropagationSubscriberBuilder builder) where T : class, ITopicProvider
    {
        builder.Services.AddSingleton<ITopicProvider, T>();

        return builder;
    }

    /// <summary>
    /// Adds the given handler to the DI container if it implements <see cref="IDomainEventHandler{TDomainEvent}"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TDomainEventHandler"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IEventPropagationSubscriberBuilder AddDomainEventHandler<TDomainEventHandler>(this IEventPropagationSubscriberBuilder builder)
    {
        var handlerType = typeof(TDomainEventHandler);
        var services = builder.Services;

        var handlerImplementsDomainEventHandlerInterface = handlerType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == DomainEventHandlerOpenGenericInterface);

        if (!handlerImplementsDomainEventHandlerInterface || !services.TryAddEventHandler(handlerType, DomainEventHandlerOpenGenericInterface))
        {
            throw new ArgumentException(Invariant($"Unable to add the handler {handlerType.FullName}. It might be abstract or not implement IDomainEventHandler<>."));
        }

        return builder;
    }

    /// <summary>
    /// Adds all the handlers from the assembly that implement IDomainEventHandler<>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static IEventPropagationSubscriberBuilder AddDomainEventHandlersFromAssembly(this IEventPropagationSubscriberBuilder builder, Assembly assembly)
    {
        var services = builder.Services;

        foreach (var handlerType in assembly.GetTypes())
        {
            services.TryAddEventHandler(handlerType, DomainEventHandlerOpenGenericInterface);
        }

        return builder;
    }

    private static bool TryAddEventHandler(this IServiceCollection services, Type handlerType, Type genericInterfaceType)
    {
        if (handlerType.IsAbstract)
        {
            return false;
        }

        var serviceTypes = handlerType.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterfaceType);

        if (!serviceTypes.Any())
        {
            return false;
        }

        foreach (var serviceType in serviceTypes)
        {
            services.TryAdd(ServiceDescriptor.Transient(serviceType, handlerType));
        }

        return true;
    }
}