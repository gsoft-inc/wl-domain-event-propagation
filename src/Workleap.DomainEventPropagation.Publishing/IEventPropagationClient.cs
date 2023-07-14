namespace Workleap.DomainEventPropagation;

public interface IEventPropagationClient
{
    Task PublishDomainEventAsync(string subject, IDomainEvent domainEvent);

    Task PublishDomainEventAsync<T>(T domainEvent) where T : IDomainEvent;

    Task PublishDomainEventsAsync(string subject, IEnumerable<IDomainEvent> domainEvents);

    Task PublishDomainEventsAsync<T>(IEnumerable<T> domainEvents) where T : IDomainEvent;
}