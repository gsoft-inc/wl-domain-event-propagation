using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

public interface IDomainEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken);
}