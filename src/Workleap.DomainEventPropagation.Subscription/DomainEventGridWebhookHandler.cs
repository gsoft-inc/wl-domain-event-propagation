using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericDomainEventHandlerMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;
    private readonly ILogger<DomainEventGridWebhookHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        ILogger<DomainEventGridWebhookHandler> logger,
        IEnumerable<ISubscriptionDomainEventBehavior> subscriptionDomainEventBehaviors)
    {
        this._serviceProvider = serviceProvider;
        this._domainEventTypeRegistry = domainEventTypeRegistry;
        this._logger = logger;
        this._pipeline = subscriptionDomainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)this.HandleDomainEventAsync, BuildPipeline);
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, ISubscriptionDomainEventBehavior pipeline)
    {
        return (events, cancellationToken) => pipeline.HandleAsync(events, next, cancellationToken);
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        // Check if it's an old Officevibe event
        if (eventGridEvent.EventType.StartsWith("Officevibe", StringComparison.Ordinal))
        {
            var domainEventType = this._domainEventTypeRegistry.GetDomainEventType(eventGridEvent.EventType);
            if (domainEventType != null)
            {
                await this.HandleOfficevibeDomainEventAsync(eventGridEvent, domainEventType, cancellationToken).ConfigureAwait(false);
                return;
            }

            this._logger.EventDomainTypeNotRegistered(eventGridEvent.EventType, eventGridEvent.Subject);
            return;
        }

        var domainEventWrapper = new DomainEventWrapper(eventGridEvent);

        var isDomainEventTypeUnknown = this._domainEventTypeRegistry.GetDomainEventType(domainEventWrapper.DomainEventName) == null;
        if (isDomainEventTypeUnknown)
        {
            this._logger.EventDomainTypeNotRegistered(domainEventWrapper.DomainEventName, eventGridEvent.Subject);
            return;
        }

        await this._pipeline(domainEventWrapper, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleDomainEventAsync(DomainEventWrapper domainEventWrapper, CancellationToken cancellationToken)
    {
        var domainEventType = this._domainEventTypeRegistry.GetDomainEventType(domainEventWrapper.DomainEventName)!;
        var domainEventHandlerType = this._domainEventTypeRegistry.GetDomainEventHandlerType(domainEventWrapper.DomainEventName)!;

        var domainEventHandler = this._serviceProvider.GetService(domainEventHandlerType);
        if (domainEventHandler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(domainEventWrapper.DomainEventName);
            return;
        }

        var domainEvent = domainEventWrapper.Unwrap(domainEventType);
        var domainEventHandlerMethod = GetHandleDomainEventAsyncMethod(domainEventHandlerType);

        await ((Task)domainEventHandlerMethod.Invoke(domainEventHandler, new[] { domainEvent, cancellationToken })!).ConfigureAwait(false);
    }

    private async Task HandleOfficevibeDomainEventAsync(EventGridEvent eventGridEvent, Type domainEventType, CancellationToken cancellationToken)
    {
        var domainEventHandlerType = this._domainEventTypeRegistry.GetDomainEventHandlerType(domainEventType.FullName!)!;
        var domainEventHandler = this._serviceProvider.GetService(domainEventHandlerType);

        if (domainEventHandler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(eventGridEvent.EventType);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(eventGridEvent.Data, domainEventType);
        var domainEventHandlerMethod = GetHandleDomainEventAsyncMethod(domainEventHandlerType);

        await ((Task)domainEventHandlerMethod.Invoke(domainEventHandler, new[] { domainEvent, cancellationToken })!).ConfigureAwait(false);
    }

    private static MethodInfo GetHandleDomainEventAsyncMethod(Type domainEventHandlerType)
    {
        return GenericDomainEventHandlerMethodCache.GetOrAdd(domainEventHandlerType, type =>
        {
            const string handleDomainEventAsyncMethodName = "HandleDomainEventAsync";
            return type.GetMethod(handleDomainEventAsyncMethodName, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"Public method {type.FullName}.{handleDomainEventAsyncMethodName} not found");
        });
    }
}