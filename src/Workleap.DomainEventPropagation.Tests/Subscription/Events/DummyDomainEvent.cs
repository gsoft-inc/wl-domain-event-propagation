namespace Workleap.DomainEventPropagation.Tests.Subscription.Events;

public sealed class DummyDomainEvent : IDomainEvent
{
    public string DataVersion => "1.0";

    public string PropertyA { get; set; } = string.Empty;

    public int PropertyB { get; set; }

    public IDictionary<string, string>? ExtensionAttributes { get; set; } = new Dictionary<string, string>();
}

internal sealed class DummyDomainEventHandler : IDomainEventHandler<DummyDomainEvent>
{
    public Task HandleDomainEventAsync(DummyDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}