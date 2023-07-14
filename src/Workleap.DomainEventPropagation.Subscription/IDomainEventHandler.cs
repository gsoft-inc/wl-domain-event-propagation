namespace Workleap.DomainEventPropagation;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleDomainEventAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}