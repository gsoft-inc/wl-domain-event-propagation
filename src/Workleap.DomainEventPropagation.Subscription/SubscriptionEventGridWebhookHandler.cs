using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class SubscriptionEventGridWebhookHandler : ISubscriptionEventGridWebhookHandler
{
    private readonly ISubscriptionTopicValidator _subscriptionTopicValidator;

    public SubscriptionEventGridWebhookHandler(
        ISubscriptionTopicValidator subscriptionTopicValidator)
    {
        this._subscriptionTopicValidator = subscriptionTopicValidator;
    }

    public SubscriptionValidationResponse HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic)
    {
        if (!this._subscriptionTopicValidator.IsSubscribedToTopic(eventTopic))
        {
            return default;
        }

        var responseData = new SubscriptionValidationResponse
        {
            ValidationResponse = subscriptionValidationEventData.ValidationCode,
        };

        return responseData;
    }
}