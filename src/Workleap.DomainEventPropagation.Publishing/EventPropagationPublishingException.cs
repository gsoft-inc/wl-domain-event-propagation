namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublishingException : Exception
{
    public EventPropagationPublishingException(string domainEventName, string topicName, string topicEndpoint, Exception innerException)
        : base($"An error occured while publishing a domain event '{domainEventName}' to the topic '{topicName}' with endpoint '{topicEndpoint}'", innerException)
    {
    }
}