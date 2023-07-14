using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

public interface IAzureSystemEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken);
}