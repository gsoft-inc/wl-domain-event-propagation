namespace Workleap.DomainEventPropagation.Tests.Subscription.Models;

public class TwoDomainEvent : IDomainEvent
{
    public string Text { get; set; }

    public int Number { get; set; }

    public string DataVersion => "1";
}