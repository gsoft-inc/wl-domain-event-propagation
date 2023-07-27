using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal interface ISubscriptionTopicValidator
{
    bool IsSubscribedToTopic(string topic);

    bool IsSubscribedToTopic(EventGridEvent eventGridEvent);
}