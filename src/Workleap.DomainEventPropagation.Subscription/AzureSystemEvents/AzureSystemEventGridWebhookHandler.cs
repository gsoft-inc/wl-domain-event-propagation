using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

internal sealed class AzureSystemEventGridWebhookHandler : IAzureSystemEventGridWebhookHandler
{
    private const string AzureSystemEventHandlerHandleMethod = "HandleAzureSystemEventAsync";

    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, MethodInfo> _handlerDictionary = new();

    public AzureSystemEventGridWebhookHandler(
        IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken)
    {
        if (EventTypeMapping.TryGetEventDataTypeForEventType(eventGridEvent.EventType, out var eventDataType))
        {
            await this.HandleAzureSystemEventAsync(systemEventData, eventDataType!, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleAzureSystemEventAsync(object eventData, Type eventDataType, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IAzureSystemEventHandler<>).MakeGenericType(eventDataType);

        var handler = this._serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            return;
        }

        var handlerMethod = this._handlerDictionary.GetOrAdd(handlerType, static type =>
        {
            return type.GetMethod(AzureSystemEventHandlerHandleMethod, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"No public method found with name {AzureSystemEventHandlerHandleMethod} on type {type.FullName}.");
        });

        await ((Task)handlerMethod.Invoke(handler, new[] { eventData, cancellationToken })!).ConfigureAwait(false);
    }
}