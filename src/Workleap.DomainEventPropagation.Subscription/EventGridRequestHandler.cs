using System.Diagnostics;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class EventGridRequestHandler : IEventGridRequestHandler
{
    private readonly IDomainEventGridWebhookHandler _domainEventGridWebhookHandler;
    private readonly IAzureSystemEventGridWebhookHandler _azureSystemEventGridWebhookHandler;
    private readonly ISubscriptionEventGridWebhookHandler _subscriptionEventGridWebhookHandler;

    public EventGridRequestHandler(
        IDomainEventGridWebhookHandler domainEventGridWebhookHandler,
        IAzureSystemEventGridWebhookHandler azureSystemEventGridWebhookHandler,
        ISubscriptionEventGridWebhookHandler subscriptionEventGridWebhookHandler)
    {
        this._domainEventGridWebhookHandler = domainEventGridWebhookHandler;
        this._azureSystemEventGridWebhookHandler = azureSystemEventGridWebhookHandler;
        this._subscriptionEventGridWebhookHandler = subscriptionEventGridWebhookHandler;
    }

    public async Task<EventGridRequestResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken)
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
                    return this.ProcessSubscriptionEvent(subscriptionValidationEventData, eventGridEvent.EventType, eventGridEvent.Topic);
                }

                await this.ProcessAzureSystemEventAsync(eventGridEvent, systemEventData, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(eventGridEvent.Topic))
            {
                await this.ProcessDomainEventAsync(eventGridEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        return new EventGridRequestResult
        {
            EventGridRequestType = EventGridRequestType.Event,
        };
    }

    private EventGridRequestResult ProcessSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic)
    {
        var response = this._subscriptionEventGridWebhookHandler.HandleEventGridSubscriptionEvent(subscriptionValidationEventData, eventType, eventTopic);

        return new EventGridRequestResult
        {
            EventGridRequestType = EventGridRequestType.Subscription,
            Response = response,
        };
    }

    private async Task ProcessDomainEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        Activity.Current?.AddBaggage("EventType", eventGridEvent.EventType);
        Activity.Current?.AddBaggage("EventTopic", eventGridEvent.Topic);
        Activity.Current?.AddBaggage("EventId", eventGridEvent.Id);

        // TODO: Assign the correlation ID to the request telemetry when OpenTelemetry is fully supported
        await this._domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessAzureSystemEventAsync(EventGridEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken)
    {
        await this._azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<EventGridEvent> GetEventGridEventsFromRequestContent(object requestContent)
    {
        var content = requestContent.ToString();
        if (content == null)
        {
            throw new ArgumentException("Request content can't be null");
        }
        
        return EventGridEvent.ParseMany(BinaryData.FromString(content));
    }
}