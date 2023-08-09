using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class SubscriptionEventGridWebhookHandler : ISubscriptionEventGridWebhookHandler
{
    public SubscriptionValidationResponse? HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic)
    {
        var responseData = new SubscriptionValidationResponse
        {
            ValidationResponse = subscriptionValidationEventData.ValidationCode,
        };

        return responseData;
    }
}