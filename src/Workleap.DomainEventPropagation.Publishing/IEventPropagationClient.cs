namespace Workleap.DomainEventPropagation;

public interface IEventPropagationClient
{
    Task PublishDomainEventAsync(string subject, IDomainEvent domainEvent, CancellationToken cancellationToken);

    Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent;

    Task PublishDomainEventsAsync(string subject, IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken);

    Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken) where T : IDomainEvent;
}