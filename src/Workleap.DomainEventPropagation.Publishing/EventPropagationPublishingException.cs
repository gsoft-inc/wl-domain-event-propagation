namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublishingException : Exception
{
    public EventPropagationPublishingException(string domainEventName, string topicEndpoint, Exception innerException)
        : base($"An error occured while publishing a domain event '{domainEventName}' to the endpoint '{topicEndpoint}'", innerException)
    {
    }
}