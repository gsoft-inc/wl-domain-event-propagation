using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.EventGrid;

namespace Workleap.EventPropagation.Subscription.AzureSystemEvents;

public interface IAzureSystemEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, object systemEventData, CancellationToken cancellationToken);
}