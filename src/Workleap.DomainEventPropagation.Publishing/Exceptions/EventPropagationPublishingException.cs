namespace Workleap.DomainEventPropagation.Exceptions;

public class EventPropagationPublishingException : Exception
{
    public EventPropagationPublishingException(string message, Exception innerException, string topicName, string subject, string topicEndpoint)
        : base(message, innerException)
    {
        this.TopicName = topicName;
        this.Subject = subject;
        this.TopicEndpoint = topicEndpoint;
    }

    public string TopicName { get; }

    public string Subject { get; }

    public string TopicEndpoint { get; }
}