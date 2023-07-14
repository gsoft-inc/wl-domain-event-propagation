using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.EventPropagation.Subscription;

public interface ISubscriptionEventGridWebhookHandler
{
    SubscriptionValidationResponse HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic);
}