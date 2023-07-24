using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

public interface IDomainEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}