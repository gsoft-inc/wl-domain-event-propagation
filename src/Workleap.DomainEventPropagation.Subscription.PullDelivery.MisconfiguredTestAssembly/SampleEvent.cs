namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.MisconfiguredTestAssembly;

[DomainEvent("an-event", EventSchema.CloudEvent)]
public class SampleEvent : IDomainEvent
{
    public string? Message { get; set; }
}

public class SampleEventTestHandler : IDomainEventHandler<SampleEvent>
{
    public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class AnotherSampleEventTestHandler : IDomainEventHandler<SampleEvent>
{
    public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}