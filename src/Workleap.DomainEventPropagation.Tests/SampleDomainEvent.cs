namespace Workleap.DomainEventPropagation.Tests;

public class SampleDomainEvent : IDomainEvent
{
    public string? Message { get; set; }
}

public class SambleDomainEventHandler : IDomainEventHandler<SampleDomainEvent>
{
    public Task HandleDomainEventAsync(SampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}