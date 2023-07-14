using System;

using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Workleap.EventPropagation.Abstractions;
using Workleap.EventPropagation.Subscription.AzureSystemEvents;

using static System.FormattableString;

namespace Workleap.EventPropagation.Subscription.Extensions;

[ExcludeFromCodeCoverage]
public static class EventPropagationSubscriberBuilderExtensions
{
    private static readonly Type DomainEventHandlerOpenGenericInterface = typeof(IDomainEventHandler<>);

    private static readonly Type AzureSystemEventHandlerOpenGenericInterface = typeof(IAzureSystemEventHandler<>);

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
    /// Adds the given handler to the DI container if it implements <see cref="IAzureSystemEventHandler{T}"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TAzureSystemEventHandler"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IEventPropagationSubscriberBuilder AddAzureSystemEventHandler<TAzureSystemEventHandler>(this IEventPropagationSubscriberBuilder builder)
    {
        var handlerType = typeof(TAzureSystemEventHandler);
        var services = builder.Services;

        var handlerImplementsAzureSystemEventHandlerInterface = handlerType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == AzureSystemEventHandlerOpenGenericInterface);

        if (!handlerImplementsAzureSystemEventHandlerInterface || !services.TryAddEventHandler(handlerType, AzureSystemEventHandlerOpenGenericInterface))
        {
            throw new ArgumentException(Invariant($"Unable to add the handler {handlerType.FullName}. It might be abstract or not implement IAzureSystemEventHandler<>."));
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

    /// <summary>
    /// Adds all handlers that implement <see cref="IAzureSystemEventHandler{T}"/> from the given assembly.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static IEventPropagationSubscriberBuilder AddAzureSystemEventHandlersFromAssembly(this IEventPropagationSubscriberBuilder builder, Assembly assembly)
    {
        var services = builder.Services;

        foreach (var handlerType in assembly.GetTypes())
        {
            services.TryAddEventHandler(handlerType, AzureSystemEventHandlerOpenGenericInterface);
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