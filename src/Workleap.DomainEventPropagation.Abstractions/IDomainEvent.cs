namespace Workleap.DomainEventPropagation;

public interface IDomainEvent
{
    string DataVersion { get; }
}
