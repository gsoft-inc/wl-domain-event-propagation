namespace Workleap.DomainEventPropagation;

internal delegate Task<HandlingStatus> DomainEventHandlerDelegate(
    DomainEventWrapper domainEventWrapper,
    CancellationToken cancellationToken);

internal interface IDomainEventBehavior
{
    Task<HandlingStatus> HandleAsync(
        DomainEventWrapper domainEventWrapper,
        DomainEventHandlerDelegate next,
        CancellationToken cancellationToken);
}