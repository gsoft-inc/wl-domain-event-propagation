namespace Workleap.DomainEventPropagation;

public delegate Task SubscriberDomainEventsHandlerDelegate(IDomainEvent domainEvent);

public interface ISubscriptionDomainEventBehavior
{
    Task Handle(IDomainEvent domainEvent, SubscriberDomainEventsHandlerDelegate next, CancellationToken cancellationToken);
}