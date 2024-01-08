using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal abstract class BaseEventHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;

    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericDomainEventHandlerMethodCache = new();

    protected BaseEventHandler(IServiceProvider serviceProvider, IDomainEventTypeRegistry domainEventTypeRegistry)
    {
        this._serviceProvider = serviceProvider;
        this._domainEventTypeRegistry = domainEventTypeRegistry;
    }

    protected bool IsDomainEventRegistrationMissing(string eventName)
    {
        return this._domainEventTypeRegistry.GetDomainEventType(eventName) == null;
    }

    protected Func<Task>? BuildDomainEventHandler(DomainEventWrapper domainEventWrapper, CancellationToken cancellationToken)
    {
        var domainEventType = this._domainEventTypeRegistry.GetDomainEventType(domainEventWrapper.DomainEventName)!;
        var domainEventHandlerType = this._domainEventTypeRegistry.GetDomainEventHandlerType(domainEventWrapper.DomainEventName)!;

        var domainEventHandler = this._serviceProvider.GetService(domainEventHandlerType);
        if (domainEventHandler == null)
        {
            return null;
        }

        var domainEvent = domainEventWrapper.Unwrap(domainEventType);

        var domainEventHandlerMethod = GenericDomainEventHandlerMethodCache.GetOrAdd(domainEventHandlerType, type =>
        {
            const string handleDomainEventAsyncMethodName = "HandleDomainEventAsync";
            return type.GetMethod(handleDomainEventAsyncMethodName, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"Public method {type.FullName}.{handleDomainEventAsyncMethodName} not found");
        });

        return () => (Task)domainEventHandlerMethod.Invoke(domainEventHandler, new[] { domainEvent, cancellationToken })!;
    }
}