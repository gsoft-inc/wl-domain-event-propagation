using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.EventGrid;

namespace Workleap.EventPropagation.Subscription;

public interface IDomainEventGridWebhookHandler
{
    Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken);
}