using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.EventPropagation.Subscription;

public sealed class EventGridRequestResult
{
    public SubscriptionValidationResponse Response { get; set; }

    public EventGridRequestType EventGridRequestType { get; set; }
}

public enum EventGridRequestType
{
    Unknown,
    Event,
    Subscription
}