namespace Workleap.DomainEventPropagation;

internal delegate Task<EventProcessingStatus> DomainEventHandlerDelegate(
    DomainEventWrapper domainEventWrapper,
    CancellationToken cancellationToken);

internal interface IDomainEventBehavior
{
    Task<EventProcessingStatus> HandleAsync(
        DomainEventWrapper domainEventWrapper,
        DomainEventHandlerDelegate next,
        CancellationToken cancellationToken);
}