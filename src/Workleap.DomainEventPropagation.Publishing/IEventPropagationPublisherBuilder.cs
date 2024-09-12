using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public interface IEventPropagationPublisherBuilder
{
    IServiceCollection Services { get; }

    IEventPropagationPublisherBuilder UseResilientEventPropagationPublisher<TEventStore>() where TEventStore : class, IEventStore;
}