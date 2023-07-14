using Azure.Messaging.EventGrid;

namespace Workleap.EventPropagation.Subscription;

public interface ISubscriptionTopicValidator
{
    bool IsSubscribedToTopic(string topic);

    bool IsSubscribedToTopic(EventGridEvent eventGridEvent);
}