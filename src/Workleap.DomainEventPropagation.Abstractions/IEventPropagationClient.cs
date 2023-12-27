namespace Workleap.DomainEventPropagation;

public interface IEventPropagationClient
{
    Task PublishDomainEventAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : IDomainEvent, new();

    Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken)
        where T : IDomainEvent, new();
}