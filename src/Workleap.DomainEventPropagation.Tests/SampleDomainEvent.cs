namespace Workleap.DomainEventPropagation.Tests;

public class SampleDomainEvent : IDomainEvent
{
    public IDictionary<string, string>? ExtensionAttributes { get; set; } = new Dictionary<string, string>();
}

public class SambleDomainEventHandler : IDomainEventHandler<SampleDomainEvent>
{
    public Task HandleDomainEventAsync(SampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}