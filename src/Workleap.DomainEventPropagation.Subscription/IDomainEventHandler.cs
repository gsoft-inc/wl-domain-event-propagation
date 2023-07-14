using System.Threading;

using System.Threading.Tasks;

using Workleap.EventPropagation.Abstractions;

namespace Workleap.EventPropagation.Subscription;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleDomainEventAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}