using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private const string DomainEventHandlerHandleMethod = "HandleDomainEventAsync";

    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ISubscriptionDomainEventBehavior> _subscriptionDomainEventBehaviors;
    private readonly ConcurrentDictionary<Type, MethodInfo> _handlerDictionary = new();

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        IEnumerable<ISubscriptionDomainEventBehavior> subscriptionDomainEventBehaviors)
    {
        this._serviceProvider = serviceProvider;
        this._subscriptionDomainEventBehaviors = subscriptionDomainEventBehaviors;
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        var domainEventType = Type.GetType(eventGridEvent.EventType, true)!;

        var domainEventWrapper = (IDomainEvent?)JsonSerializer.Deserialize(eventGridEvent.Data.ToString(), typeof(DomainEventWrapper), SerializerOptions);
        if (domainEventWrapper == null)
        {
            throw new InvalidOperationException($"Can't deserialize eventGrid event with Id: {eventGridEvent.Id}; Subject: {eventGridEvent.Subject}; EventType: {eventGridEvent.EventType}.");
        }

        await this.HandleDomainEventAsync(domainEventWrapper, domainEventType, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleDomainEventAsync(IDomainEvent domainEventWrapper, Type domainEventType, CancellationToken cancellationToken)
    {
        async Task Handler(IDomainEvent domainEvent)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);

            var handler = this._serviceProvider.GetService(handlerType);

            if (handler == null)
            {
                return;
            }

            var handlerMethod = this._handlerDictionary.GetOrAdd(handlerType, static type =>
            {
                return type.GetMethod(DomainEventHandlerHandleMethod, BindingFlags.Public | BindingFlags.Instance) ??
                       throw new InvalidOperationException($"No public method found with name {DomainEventHandlerHandleMethod} on type {type.FullName}.");
            });

            await ((Task)handlerMethod.Invoke(handler, new object[] { domainEvent!, cancellationToken })!).ConfigureAwait(false);
        }

        var accumulator = this._subscriptionDomainEventBehaviors
            .Reverse()
            .Aggregate(
                Handler,
                (SubscriberDomainEventsHandlerDelegate next, ISubscriptionDomainEventBehavior pipeline) => (events) => pipeline.Handle(events, next, cancellationToken));

        await accumulator(domainEventWrapper).ConfigureAwait(false);
    }
}