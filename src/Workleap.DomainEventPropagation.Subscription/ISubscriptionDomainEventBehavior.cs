namespace Workleap.DomainEventPropagation;

internal delegate Task DomainEventHandlerDelegate(DomainEventWrapper domainEventWrapper, CancellationToken cancellationToken);

internal interface ISubscriptionDomainEventBehavior
{
    Task Handle(DomainEventWrapper domainEventWrapper, DomainEventHandlerDelegate next, CancellationToken cancellationToken);
}