using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.ApplicationInsights.DataContracts;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation;

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

        foreach (var eventGridEvent in GetEventGridEventsFromRequestContent(requestContent))
        {
            if (eventGridEvent.TryGetSystemEventData(out var systemEventData))
            {
                if (systemEventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    return ProcessSubscriptionEvent(subscriptionValidationEventData, eventGridEvent.Type, eventGridEvent.DataSchema);
                }

                await this.ProcessAzureSystemEventAsync(eventGridEvent, systemEventData, requestTelemetry, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(eventGridEvent.DataSchema))
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

    private async Task ProcessDomainEventAsync(CloudEvent eventGridEvent, RequestTelemetry requestTelemetry, CancellationToken cancellationToken)
    {
        Activity.Current?.AddBaggage("EventType", eventGridEvent.Type);
        Activity.Current?.AddBaggage("EventTopic", eventGridEvent.DataSchema);
        Activity.Current?.AddBaggage("EventId", eventGridEvent.Id);

        // TODO: Assign the correlation ID to the request telemetry when OpenTelemetry is fully supported
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

    private async Task ProcessAzureSystemEventAsync(CloudEvent eventGridEvent, object systemEventData, RequestTelemetry requestTelemetry, CancellationToken cancellationToken)
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

    private static IEnumerable<CloudEvent> GetEventGridEventsFromRequestContent(object requestContent)
    {
        return CloudEvent.ParseMany(BinaryData.FromString(requestContent.ToString()));
    }

    private static void SetRequestTelemetrySuccessStatus(RequestTelemetry requestTelemetry, bool status)
    {
        if (requestTelemetry != null)
        {
            requestTelemetry.Success = status;
        }
    }
}