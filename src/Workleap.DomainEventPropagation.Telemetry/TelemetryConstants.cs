namespace Workleap.DomainEventPropagation;

public static class TelemetryConstants
{
    public const string DomainEventsPropagationFailed = "DomainEventsPropagationFailed";
    public const string DomainEventsPropagated = "DomainEventsPropagated";

    public const string DomainEventRejectedBasedOnTopic = "DomainEventRejected";
    public const string DomainEventDeserializationFailed = "DomainEventDeserializationFailed";
    public const string DomainEventNoHandlerFound = "DomainEventNoHandlerFound";
    public const string DomainEventHandled = "DomainEventHandled";

    public const string AzureSystemEventRejectedBasedOnTopic = "AzureSystemEventRejected";
    public const string AzureSystemEventDeserializationFailed = "AzureSystemEventDeserializationFailed";
    public const string AzureSystemEventNoHandlerFound = "AzureSystemEventNoHandlerFound";
    public const string AzureSystemEventHandled = "AzureSystemEventHandled";

    public const string SubscriptionEventReceivedAndIgnored = "SubscriptionEventReceivedAndIgnored";
    public const string SubscriptionEventReceivedAndAccepted = "SubscriptionEventReceivedAndAccepted";

}