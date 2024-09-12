namespace Workleap.DomainEventPropagation;

internal interface IEventPropagationDispatcher
{
    Task DispatchDomainEventsAsync(DomainEventWrapperCollection domainEventWrappers, CancellationToken cancellationToken);
}
