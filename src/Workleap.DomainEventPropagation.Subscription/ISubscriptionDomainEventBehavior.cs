namespace Workleap.DomainEventPropagation;

internal delegate Task SubscriberDomainEventsHandlerDelegate(IDomainEvent domainEvent);

internal interface ISubscriptionDomainEventBehavior
{
    Task Handle(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}