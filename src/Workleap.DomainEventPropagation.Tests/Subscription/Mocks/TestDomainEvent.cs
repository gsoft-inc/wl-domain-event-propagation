namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

public class TestDomainEvent : IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }

    public string DataVersion => "1";

    public IDictionary<string, string>? ExtensionAttributes { get; set; } = new Dictionary<string, string>();
}