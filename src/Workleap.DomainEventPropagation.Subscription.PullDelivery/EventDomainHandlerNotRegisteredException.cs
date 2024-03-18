namespace Workleap.DomainEventPropagation;

internal sealed class EventDomainHandlerNotRegisteredException : Exception
{
    public EventDomainHandlerNotRegisteredException(string domainEventName)
        : base($"A handler for the domain event {domainEventName} was not registered")
    {
    }
}