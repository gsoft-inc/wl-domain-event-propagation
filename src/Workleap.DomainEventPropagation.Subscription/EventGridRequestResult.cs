using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

public sealed class EventGridRequestResult
{
    public SubscriptionValidationResponse? Response { get; init; }

    public EventGridRequestType EventGridRequestType { get; init; }
}