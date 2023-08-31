namespace Workleap.DomainEventPropagation;

internal delegate Task DomainEventsHandlerDelegate(IEnumerable<DomainEventWrapper> events);

internal interface IPublishingDomainEventBehavior
{
    Task Handle(IEnumerable<DomainEventWrapper> events, DomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}