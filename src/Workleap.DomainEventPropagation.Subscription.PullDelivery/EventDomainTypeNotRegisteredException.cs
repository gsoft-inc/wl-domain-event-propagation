namespace Workleap.DomainEventPropagation;

internal sealed class EventDomainTypeNotRegisteredException : Exception
{
    public EventDomainTypeNotRegisteredException(string domainEventName, string subject)
        : base($"The domain event type {domainEventName} could not be found for event with subject {subject}")
    {
    }
}