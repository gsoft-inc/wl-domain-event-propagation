namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests.Events;

[DomainEvent("sample-event-that-causes-exception", EventSchema.CloudEvent)]
public class SampleEventThatCausesException : IDomainEvent
{
    public string? Message { get; set; }
}

public class FailingHandler : IDomainEventHandler<SampleEventThatCausesException>
{
    public Task HandleDomainEventAsync(SampleEventThatCausesException domainEvent, CancellationToken cancellationToken)
    {
        throw new Exception();
    }
}