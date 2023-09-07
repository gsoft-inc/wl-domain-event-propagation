using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation;

public interface IEventPropagationPublisherBuilder
{
    IServiceCollection Services { get; }
}