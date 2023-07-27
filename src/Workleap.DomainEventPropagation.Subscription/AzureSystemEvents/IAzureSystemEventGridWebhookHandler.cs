using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

internal interface IAzureSystemEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken);
}