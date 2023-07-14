using Azure.Messaging.EventGrid.SystemEvents;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents.Mocks;

public sealed class RegistrationTestAzureSystemEventHandlerTwo : IAzureSystemEventHandler<MapsGeofenceEnteredEventData>
{
    public Task HandleAzureSystemEventAsync(MapsGeofenceEnteredEventData azureSystemEvent, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}