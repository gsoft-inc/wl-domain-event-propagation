namespace Workleap.DomainEventPropagation;

internal delegate Task DomainEventsHandlerDelegate(DomainEventWrapperCollection domainEventWrappers, CancellationToken cancellationToken);

internal interface IPublishingDomainEventBehavior
{
    Task Handle(DomainEventWrapperCollection domainEventWrappers, DomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}