using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal interface IDomainEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken);

    Task HandleEventGridWebhookEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}