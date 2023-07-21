namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

public class TestExceptionDomainEvent : IDomainEvent
{
    public string Text { get; set; }

    public int Number { get; set; }

    public string DataVersion => "1";
}