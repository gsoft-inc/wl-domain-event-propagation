using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

public interface IDomainEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(CloudEvent eventGridEvent, CancellationToken cancellationToken);
}