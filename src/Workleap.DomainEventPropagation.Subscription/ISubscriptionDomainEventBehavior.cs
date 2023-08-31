namespace Workleap.DomainEventPropagation;

internal delegate Task SubscriberDomainEventsHandlerDelegate(DomainEventWrapper domainEvent);

internal interface ISubscriptionDomainEventBehavior
{
    Task Handle(DomainEventWrapper domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}