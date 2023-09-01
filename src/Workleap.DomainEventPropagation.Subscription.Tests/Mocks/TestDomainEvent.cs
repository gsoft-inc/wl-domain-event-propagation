namespace Workleap.DomainEventPropagation.Subscription.Tests.Mocks;

[DomainEvent("test")]
public class TestDomainEvent : IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }
}