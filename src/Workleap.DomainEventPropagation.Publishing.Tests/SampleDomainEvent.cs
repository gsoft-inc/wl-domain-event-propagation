namespace Workleap.DomainEventPropagation.Tests;

public class SampleDomainEvent : IDomainEvent
{
    public string? Message { get; set; }
}

public class SampleDomainEventHandler : IDomainEventHandler<SampleDomainEvent>
{
    public Task HandleDomainEventAsync(SampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}