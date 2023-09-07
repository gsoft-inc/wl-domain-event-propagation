using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation;

internal sealed class EventGridRequestResult
{
    public EventGridRequestResult(EventGridRequestType requestType, SubscriptionValidationResponse? validationResponse = null)
    {
        this.RequestType = requestType;
        this.ValidationResponse = validationResponse;
    }

    public EventGridRequestType RequestType { get; }

    public SubscriptionValidationResponse? ValidationResponse { get; }
}