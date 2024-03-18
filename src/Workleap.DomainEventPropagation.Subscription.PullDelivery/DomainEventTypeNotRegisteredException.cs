namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventTypeNotRegisteredException : Exception
{
    public DomainEventTypeNotRegisteredException(string domainEventName)
        : base($"The domain event type {domainEventName} could not be found for event with subject")
    {
    }
}