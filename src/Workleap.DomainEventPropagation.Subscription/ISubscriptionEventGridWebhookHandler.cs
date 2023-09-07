using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal interface ISubscriptionEventGridWebhookHandler
{
    SubscriptionValidationResponse? HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData);
}