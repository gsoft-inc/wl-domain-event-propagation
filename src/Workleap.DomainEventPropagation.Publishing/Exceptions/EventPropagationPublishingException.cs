namespace Workleap.DomainEventPropagation.Exceptions;

public class EventPropagationPublishingException : Exception
{
    public EventPropagationPublishingException(string eventName, string topicName, string topicEndpoint, Exception innerException)
        : base($"An error occured while publishing an event '{eventName}' to the topic {topicName} and endpoint {topicEndpoint}", innerException)
    {
    }
}