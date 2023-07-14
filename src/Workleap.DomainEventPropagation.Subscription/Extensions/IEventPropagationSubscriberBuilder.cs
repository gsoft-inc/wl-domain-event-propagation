using Microsoft.Extensions.DependencyInjection;

namespace Workleap.EventPropagation.Subscription.Extensions;

public interface IEventPropagationSubscriberBuilder
{
    IServiceCollection Services { get; }
}