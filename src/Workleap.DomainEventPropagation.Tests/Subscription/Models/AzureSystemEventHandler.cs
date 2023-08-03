using Azure.Messaging.EventGrid.SystemEvents;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Models;

public sealed class AzureSystemEventHandler : IAzureSystemEventHandler<MediaJobErroredEventData>,
    IAzureSystemEventHandler<MediaJobFinishedEventData>
{
    public Task HandleAzureSystemEventAsync(MediaJobErroredEventData azureSystemEvent, CancellationToken cancellationToken)
    {
        throw new Exception("HandleAzureSystemEventAsync called for MediaJobErroredEventData");
    }

    public Task HandleAzureSystemEventAsync(MediaJobFinishedEventData azureSystemEvent, CancellationToken cancellationToken)
    {
        throw new Exception("HandleAzureSystemEventAsync called for MediaJobFinishedEventData");
    }
}