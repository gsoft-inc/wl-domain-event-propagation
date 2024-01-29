using System.Collections.Concurrent;
using System.Reflection;

namespace Workleap.DomainEventPropagation;

internal abstract class BaseEventHandler
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericDomainEventHandlerMethodCache = new();

    protected abstract Type? GetDomainEventType(string domainEventName);

    protected abstract Type? GetDomainEventHandlerType(string domainEventName);

    protected abstract object? ResolveDomainEventHandler(Type domainEventHandlerType);

    protected Func<Task>? BuildDomainEventHandler(DomainEventWrapper domainEventWrapper, CancellationToken cancellationToken)
    {
        var domainEventType = this.GetDomainEventType(domainEventWrapper.DomainEventName);
        var domainEventHandlerType = this.GetDomainEventHandlerType(domainEventWrapper.DomainEventName)!;

        var domainEventHandler = this.ResolveDomainEventHandler(domainEventHandlerType);
        if (domainEventHandler == null)
        {
            return null;
        }

        var domainEvent = domainEventWrapper.Unwrap(domainEventType!);

        var domainEventHandlerMethod = GenericDomainEventHandlerMethodCache.GetOrAdd(domainEventHandlerType, type =>
        {
            const string handleDomainEventAsyncMethodName = "HandleDomainEventAsync";
            return type.GetMethod(handleDomainEventAsyncMethodName, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"Public method {type.FullName}.{handleDomainEventAsyncMethodName} not found");
        });

        return () => (Task)domainEventHandlerMethod.Invoke(domainEventHandler, new[] { domainEvent, cancellationToken })!;
    }
}