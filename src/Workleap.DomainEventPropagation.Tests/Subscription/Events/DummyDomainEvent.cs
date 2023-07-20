using Workleap.DomainEventPropagation;

public sealed class DummyDomainEvent : IDomainEvent
{
    public string DataVersion => "1.0";

    public string PropertyA { get; set; }

    public int PropertyB { get; set; }
}

internal sealed class DummyDomainEventHandler : IDomainEventHandler<DummyDomainEvent>
{
    public Task HandleDomainEventAsync(DummyDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}