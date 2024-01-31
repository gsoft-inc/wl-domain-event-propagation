using System.Reflection;

namespace Workleap.DomainEventPropagation;

public interface IEventPropagationSubscriberBuilder
{
    IEventPropagationSubscriberBuilder AddTopicSubscription();

    IEventPropagationSubscriberBuilder AddTopicSubscription(string optionsSectionName);

    IEventPropagationSubscriberBuilder AddTopicSubscription(string optionsSectionName, Action<EventPropagationSubscriptionOptions> configureOptions);
}