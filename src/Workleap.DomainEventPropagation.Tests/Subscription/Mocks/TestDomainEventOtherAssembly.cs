using Workleap.DomainEventPropagation;

// ReSharper disable once CheckNamespace
namespace Workleap.AnotherAssembly.DomainEvents;

public class TestDomainEventOtherAssembly : IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }

    public string DataVersion => "1";
}