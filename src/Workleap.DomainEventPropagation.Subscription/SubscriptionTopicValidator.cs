using System.Collections.Generic;

using Azure.Messaging.EventGrid;

using Microsoft.Extensions.Options;

using Workleap.EventPropagation.Abstractions;

namespace Workleap.EventPropagation.Subscription;

internal sealed class SubscriptionTopicValidator : ISubscriptionTopicValidator
{
    private readonly IEnumerable<ITopicProvider> _topicProviders;
    private readonly EventPropagationSubscriberOptions _eventPropagationSubscriberOptions;

    public SubscriptionTopicValidator(IOptions<EventPropagationSubscriberOptions> eventPropagationSubscriberOptions, IEnumerable<ITopicProvider> topicProviders)
    {
        this._topicProviders = topicProviders;
        this._eventPropagationSubscriberOptions = eventPropagationSubscriberOptions.Value;
    }

    public bool IsSubscribedToTopic(string topic)
    {
        foreach (var subscribedTopic in this._eventPropagationSubscriberOptions.SubscribedTopics)
        {
            foreach(var topicProvider in this._topicProviders)
            {
                var topicValidationPattern = topicProvider.GetTopicValidationPattern(subscribedTopic);
                if (topic.ToLowerInvariant().Contains(topicValidationPattern.ToLowerInvariant()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsSubscribedToTopic(EventGridEvent eventGridEvent)
    {
        return this.IsSubscribedToTopic(eventGridEvent.Topic);
    }
}