using Workleap.DomainEventPropagation;

// ReSharper disable once CheckNamespace
namespace Workleap.AnotherAssembly.DomainEvents;

public class TestDomainEvent : IDomainEvent
{
    public string Text { get; set; }

    public int Number { get; set; }

    public string DataVersion => "1";
}