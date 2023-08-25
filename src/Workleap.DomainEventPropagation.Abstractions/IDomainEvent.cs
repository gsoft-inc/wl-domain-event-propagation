namespace Workleap.DomainEventPropagation;

public interface IDomainEvent
{
    IDictionary<string, string>? ExtensionAttributes { get; set; }
}
