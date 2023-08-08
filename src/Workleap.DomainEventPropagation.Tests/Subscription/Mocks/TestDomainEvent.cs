namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

public class TestDomainEvent : IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }

    public string DataVersion => "1";
}