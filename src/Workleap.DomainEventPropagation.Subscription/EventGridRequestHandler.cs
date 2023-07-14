using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;

using Workleap.EventPropagation.Subscription.AzureSystemEvents;
using Workleap.EventPropagation.Telemetry;

namespace Workleap.EventPropagation.Subscription;

internal sealed class EventGridRequestHandler : IEventGridRequestHandler
{
    private readonly IDomainEventGridWebhookHandler _domainEventGridWebhookHandler;
    private readonly IAzureSystemEventGridWebhookHandler _azureSystemEventGridWebhookHandler;
    private readonly ISubscriptionEventGridWebhookHandler _subscriptionEventGridWebhookHandler;
    private readonly ITelemetryClientProvider _telemetryClientProvider;

    public EventGridRequestHandler(
        IDomainEventGridWebhookHandler domainEventGridWebhookHandler,
        IAzureSystemEventGridWebhookHandler azureSystemEventGridWebhookHandler,
        ISubscriptionEventGridWebhookHandler subscriptionEventGridWebhookHandler,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._domainEventGridWebhookHandler = domainEventGridWebhookHandler;
        this._azureSystemEventGridWebhookHandler = azureSystemEventGridWebhookHandler;
        this._subscriptionEventGridWebhookHandler = subscriptionEventGridWebhookHandler;
        this._telemetryClientProvider = telemetryClientProvider;
    }

    public async Task<EventGridRequestResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken, RequestTelemetry requestTelemetry = default)
    {
        if (requestContent == null)
        {
            throw new ArgumentException("Request content cannot be null.");
        }

        var eventGridEvents = this.GetEventGridEventsFromRequestContent(requestContent);

        foreach (var eventGridEvent in eventGridEvents)
        {
            if (eventGridEvent.TryGetSystemEventData(out var systemEventData))
            {
                if (systemEventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    return ProcessSubscriptionEvent(subscriptionValidationEventData, eventGridEvent.EventType, eventGridEvent.Topic);
                }

                await this.ProcessAzureSystemEventAsync(eventGridEvent, systemEventData, requestTelemetry, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(eventGridEvent.Topic))
            {
                await this.ProcessDomainEventAsync(eventGridEvent, requestTelemetry, cancellationToken);
            }
        }

        return new EventGridRequestResult
        {
            EventGridRequestType = EventGridRequestType.Event
        };
    }

    private EventGridRequestResult ProcessSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic)
    {
        try
        {
            var response = this._subscriptionEventGridWebhookHandler.HandleEventGridSubscriptionEvent(subscriptionValidationEventData, eventType, eventTopic);

            return new EventGridRequestResult
            {
                EventGridRequestType = EventGridRequestType.Subscription,
                Response = response
            };
        }
        catch (Exception ex)
        {
            this._telemetryClientProvider.TrackException(ex);

            throw;
        }
    }

    private async Task ProcessDomainEventAsync(EventGridEvent eventGridEvent, RequestTelemetry requestTelemetry, CancellationToken cancellationToken)
    {
        Activity.Current?.AddBaggage("EventType", eventGridEvent.EventType);
        Activity.Current?.AddBaggage("EventTopic", eventGridEvent.Topic);
        Activity.Current?.AddBaggage("EventId", eventGridEvent.Id);

        SetTelemetryCorrelationId(requestTelemetry, eventGridEvent);
        var operation = this._telemetryClientProvider.StartOperation(requestTelemetry);

        try
        {
            await this._domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, cancellationToken);

            SetRequestTelemetrySuccessStatus(requestTelemetry: requestTelemetry, status: true);
        }
        catch (Exception ex)
        {
            this._telemetryClientProvider.TrackException(ex);

            SetRequestTelemetrySuccessStatus(requestTelemetry: requestTelemetry, status: false);

            throw;
        }
        finally
        {
            this._telemetryClientProvider.StopOperation(operation);
        }
    }

    private async Task ProcessAzureSystemEventAsync(EventGridEvent eventGridEvent, object systemEventData, RequestTelemetry requestTelemetry, CancellationToken cancellationToken)
    {
        var operation = this._telemetryClientProvider.StartOperation(requestTelemetry);

        try
        {
            await this._azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, cancellationToken);

            SetRequestTelemetrySuccessStatus(requestTelemetry: requestTelemetry, status: true);
        }
        catch (Exception ex)
        {
            this._telemetryClientProvider.TrackException(ex);

            SetRequestTelemetrySuccessStatus(requestTelemetry: requestTelemetry, status: false);

            throw;
        }
        finally
        {
            this._telemetryClientProvider.StopOperation(operation);
        }
    }

    private EventGridEvent[] GetEventGridEventsFromRequestContent(object requestContent)
    {
        try
        {
            return EventGridEvent.ParseMany(BinaryData.FromString(requestContent.ToString()));
        }
        catch (Exception ex)
        {
            this._telemetryClientProvider.TrackEvent("InitialDeserializationFailed", $"Initial EventGridEvent deserialization failed: {ex.Message}", null);
            // Mismatch in lib versions between publisher and subscriber can cause event data interpretation as double serialization
            // fall back on basic string deserialization instead of binary and see if it works before throwing exception
            return JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent.ToString());
        }
    }

    private static void SetTelemetryCorrelationId(RequestTelemetry requestTelemetry, EventGridEvent eventGridEvent)
    {
        if (requestTelemetry != null && string.IsNullOrEmpty(requestTelemetry.Context.Operation.ParentId))
        {
            requestTelemetry.Context.Operation.ParentId = TelemetryHelper.GetOperationCorrelationIdFromSerializedObject(eventGridEvent.Data.ToString());
        }
    }

    private static void SetRequestTelemetrySuccessStatus(RequestTelemetry requestTelemetry, bool status)
    {
        if (requestTelemetry != null)
        {
            requestTelemetry.Success = status;
        }
    }
}