namespace Workleap.DomainEventPropagation;

internal sealed class EventDomainHandlerNotRegistered : Exception
{
    public EventDomainHandlerNotRegistered(string domainEventName)
        : base($"A handler for the domain event {domainEventName} was not registered")
    {
    }
}