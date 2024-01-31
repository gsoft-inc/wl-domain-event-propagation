namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.MisconfiguredTestAssembly;

[DomainEvent("an-event", EventSchema.CloudEvent)]
public class SampleEvent : IDomainEvent
{
    public string? Message { get; set; }
}

public class TestHandler : IDomainEventHandler<SampleEvent>
{
    public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class AnotherTestHandler : IDomainEventHandler<SampleEvent>
{
    public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}