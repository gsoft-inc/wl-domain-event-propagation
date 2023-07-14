using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Extensions;

public interface IEventPropagationSubscriberBuilder
{
    IServiceCollection Services { get; }
}