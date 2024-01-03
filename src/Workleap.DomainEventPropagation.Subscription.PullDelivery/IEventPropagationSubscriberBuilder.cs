using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public interface IEventPropagationSubscriberBuilder
{
    IServiceCollection Services { get; }

    internal void ConfigureSubscriber(Action<EventPropagationSubscriptionOptions> configure, string optionsSectionName);
}