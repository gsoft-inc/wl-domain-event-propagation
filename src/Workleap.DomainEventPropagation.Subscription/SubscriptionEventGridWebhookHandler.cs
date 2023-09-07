using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class SubscriptionEventGridWebhookHandler : ISubscriptionEventGridWebhookHandler
{
    public SubscriptionValidationResponse HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData)
    {
        return new SubscriptionValidationResponse
        {
            ValidationResponse = subscriptionValidationEventData.ValidationCode,
        };
    }
}