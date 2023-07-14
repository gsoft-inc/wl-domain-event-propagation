namespace Workleap.DomainEventPropagation.Exceptions;

public class EventPropagationPublishingException : Exception
{
    public EventPropagationPublishingException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public string TopicName { get; set; }

    public string Subject { get; set; }

    public string TopicEndpoint { get; set; }
}