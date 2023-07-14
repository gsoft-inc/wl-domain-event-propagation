namespace Workleap.DomainEventPropagation.Tests.Subscription.Models;

public sealed class DomainEventHandler : IDomainEventHandler<TwoDomainEvent>,
    IDomainEventHandler<OneDomainEvent>

{
    public Task HandleDomainEventAsync(OneDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        throw new Exception("HandleDomainEventAsync called for OneDomainEvent");
    }

    public Task HandleDomainEventAsync(TwoDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        throw new Exception("HandleDomainEventAsync called for TwoDomainEvent");
    }
}