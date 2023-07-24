using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

public interface IAzureSystemEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(CloudEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken);
}