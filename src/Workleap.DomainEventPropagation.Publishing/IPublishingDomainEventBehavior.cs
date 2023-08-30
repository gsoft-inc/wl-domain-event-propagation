namespace Workleap.DomainEventPropagation;

internal delegate Task DomainEventsHandlerDelegate(IEnumerable<IDomainEvent> events);

internal interface IPublishingDomainEventBehavior
{
    Task Handle(IEnumerable<IDomainEvent> events, DomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}