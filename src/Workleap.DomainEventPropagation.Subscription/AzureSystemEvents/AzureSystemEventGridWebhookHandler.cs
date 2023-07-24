using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

internal sealed class AzureSystemEventGridWebhookHandler : IAzureSystemEventGridWebhookHandler
{
    private const string AzureSystemEventHandlerHandleMethod = "HandleAzureSystemEventAsync";

    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriptionTopicValidator _subscriptionTopicValidator;
    private readonly ITelemetryClientProvider _telemetryClientProvider;
    private readonly ConcurrentDictionary<Type, MethodInfo> _handlerDictionary = new();

    public AzureSystemEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        ISubscriptionTopicValidator subscriptionTopicValidator,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._serviceProvider = serviceProvider;
        this._subscriptionTopicValidator = subscriptionTopicValidator;
        this._telemetryClientProvider = telemetryClientProvider;
    }

    public async Task HandleEventGridWebhookEventAsync(CloudEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken)
    {
        if (!this._subscriptionTopicValidator.IsSubscribedToTopic(eventGridEvent.DataSchema))
        {
            this._telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventRejectedBasedOnTopic, $"Azure System event received and ignored based on topic. Topic: ­{eventGridEvent.DataSchema}", eventGridEvent.Type);

            return;
        }

        if (EventTypeMapping.TryGetEventDataTypeForEventType(eventGridEvent.Type, out var eventDataType))
        {
            await this.HandleAzureSystemEventAsync(systemEventData, eventGridEvent.Type, eventDataType, cancellationToken);

            return;
        }

        this._telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventDeserializationFailed, $"Azure System event received. Cannot deserialize object", eventGridEvent.Type);
    }

    private async Task HandleAzureSystemEventAsync(object eventData, string eventGridEventType, Type eventDataType, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IAzureSystemEventHandler<>).MakeGenericType(eventDataType);

        var handler = this._serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            this._telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventNoHandlerFound, $"No Azure System event handler found of type {handlerType.FullName}.", eventGridEventType);

            return;
        }

        this._telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventHandled, $"Azure System event received and matched with event handler: {handlerType}", eventGridEventType);

        var handlerMethod = this._handlerDictionary.GetOrAdd(handlerType, static type =>
        {
            return type.GetMethod(AzureSystemEventHandlerHandleMethod, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"No public method found with name {AzureSystemEventHandlerHandleMethod} on type {type.FullName}.");
        });

        await (Task)handlerMethod.Invoke(handler, new object[] { eventData, cancellationToken });
    }
}