using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class SubscriptionEventGridWebhookHandler : ISubscriptionEventGridWebhookHandler
{
    private readonly ISubscriptionTopicValidator _subscriptionTopicValidator;
    private readonly ITelemetryClientProvider _telemetryClientProvider;

    public SubscriptionEventGridWebhookHandler(
        ISubscriptionTopicValidator subscriptionTopicValidator,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._subscriptionTopicValidator = subscriptionTopicValidator;
        this._telemetryClientProvider = telemetryClientProvider;
    }

    public SubscriptionValidationResponse HandleEventGridSubscriptionEvent(SubscriptionValidationEventData subscriptionValidationEventData, string eventType, string eventTopic)
    {
        if (!this._subscriptionTopicValidator.IsSubscribedToTopic(eventTopic))
        {
            this._telemetryClientProvider.TrackEvent(TelemetryConstants.SubscriptionEventReceivedAndIgnored, $"Subscription event received and ignored based on topic", eventType);

            return default;
        }

        this._telemetryClientProvider.TrackEvent(TelemetryConstants.SubscriptionEventReceivedAndAccepted, $"Subscription event received and accepted for topic {eventTopic}", eventType);

        var responseData = new SubscriptionValidationResponse
        {
            ValidationResponse = subscriptionValidationEventData.ValidationCode
        };

        return responseData;
    }
}