using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Extensions;

public interface IEventPropagationPublisherBuilder
{
    IServiceCollection Services { get; }
}