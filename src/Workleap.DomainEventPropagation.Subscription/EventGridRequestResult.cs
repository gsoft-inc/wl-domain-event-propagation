using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

public sealed class EventGridRequestResult
{
    public SubscriptionValidationResponse Response { get; set; }

    public EventGridRequestType EventGridRequestType { get; set; }
}

public enum EventGridRequestType
{
    Unknown,
    Event,
    Subscription,
}