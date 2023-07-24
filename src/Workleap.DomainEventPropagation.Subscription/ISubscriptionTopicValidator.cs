using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

public interface ISubscriptionTopicValidator
{
    bool IsSubscribedToTopic(string topic);

    bool IsSubscribedToTopic(EventGridEvent cloudEvent);
}