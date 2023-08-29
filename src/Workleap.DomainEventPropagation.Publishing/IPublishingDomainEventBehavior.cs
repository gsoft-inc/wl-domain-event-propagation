namespace Workleap.DomainEventPropagation;

public delegate Task DomainEventsHandlerDelegate(IEnumerable<IDomainEvent> events);

public interface IPublishingDomainEventBehavior
{
    Task Handle(IEnumerable<IDomainEvent> events, DomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}