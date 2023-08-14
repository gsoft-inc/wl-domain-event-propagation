using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private const string DomainEventHandlerHandleMethod = "HandleDomainEventAsync";

    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, MethodInfo> _handlerDictionary = new();

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        var domainEventType = Type.GetType(eventGridEvent.EventType, true)!;

        var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(eventGridEvent.Data.ToString(), domainEventType, SerializerOptions);
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Can't deserialize event Id: {eventGridEvent.Id}; Subject: {eventGridEvent.Subject}; Data version: {eventGridEvent.DataVersion}.");
        }

        await this.HandleDomainEventAsync(domainEvent, domainEventType, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleDomainEventAsync(IDomainEvent domainEvent, Type domainEventType, CancellationToken cancellationToken)
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

        await ((Task)handlerMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!).ConfigureAwait(false);
    }
}