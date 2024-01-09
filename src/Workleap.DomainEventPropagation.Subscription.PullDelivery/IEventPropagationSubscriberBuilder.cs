using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public interface IEventPropagationSubscriberBuilder
{
    IServiceCollection Services { get; }

    IEventPropagationSubscriberBuilder AddDomainEventHandlers(Assembly assembly);

    IEventPropagationSubscriberBuilder AddDomainEventHandler<TEvent, THandler>()
        where THandler : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent;
}