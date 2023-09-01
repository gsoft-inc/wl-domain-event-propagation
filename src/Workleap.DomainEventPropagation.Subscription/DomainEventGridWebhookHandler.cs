using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private readonly ConcurrentDictionary<Type, MethodInfo> GenericDomainEventHandlerMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;
    private readonly DomainEventHandlerDelegate _pipeline;

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        IEnumerable<ISubscriptionDomainEventBehavior> subscriptionDomainEventBehaviors)
    {
        this._serviceProvider = serviceProvider;
        this._domainEventTypeRegistry = domainEventTypeRegistry;
        this._pipeline = subscriptionDomainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)this.HandleDomainEventAsync, BuildPipeline);
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, ISubscriptionDomainEventBehavior pipeline)
    {
        return (events, cancellationToken) => pipeline.HandleAsync(events, next, cancellationToken);
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        var domainEventWrapper = new DomainEventWrapper(eventGridEvent.Data.ToObjectFromJson<JsonObject>());

        var isDomainEventTypeUnknown = this._domainEventTypeRegistry.GetDomainEventType(domainEventWrapper.DomainEventName) == null;
        if (isDomainEventTypeUnknown)
        {
            // TODO log info message instead of throwing
            throw new InvalidOperationException($"Can't find domain event type for event with name: {domainEventWrapper.DomainEventName}; Subject: {eventGridEvent.Subject}; EventType: {eventGridEvent.EventType}.");
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
            // TODO log info message
            return;
        }

        var domainEvent = domainEventWrapper.Unwrap(domainEventType);

        var domainEventHandlerMethod = this.GenericDomainEventHandlerMethodCache.GetOrAdd(domainEventHandlerType, type =>
        {
            const string handleDomainEventAsyncMethodName = "HandleDomainEventAsync";
            return type.GetMethod(handleDomainEventAsyncMethodName, BindingFlags.Public | BindingFlags.Instance) ??
                throw new InvalidOperationException($"Public method {type.FullName}.{handleDomainEventAsyncMethodName} not found");
        });

        await ((Task)domainEventHandlerMethod.Invoke(domainEventHandler, new[] { domainEvent, cancellationToken })!).ConfigureAwait(false);
    }
}