using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

public interface ISubscriptionEventGridWebhookHandler
{
    SubscriptionValidationResponse HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic);
}