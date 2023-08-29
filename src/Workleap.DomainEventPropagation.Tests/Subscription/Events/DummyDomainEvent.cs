namespace Workleap.DomainEventPropagation.Tests.Subscription.Events;

public sealed class DummyDomainEvent : IDomainEvent
{
    public string PropertyA { get; set; } = string.Empty;

    public int PropertyB { get; set; }
}

internal sealed class DummyDomainEventHandler : IDomainEventHandler<DummyDomainEvent>
{
    public Task HandleDomainEventAsync(DummyDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}