using Azure.Messaging.EventGrid.SystemEvents;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents.Mocks;

public sealed class RegistrationTestAzureSystemEventHandlerOne : IAzureSystemEventHandler<MapsGeofenceExitedEventData>
{
    public Task HandleAzureSystemEventAsync(MapsGeofenceExitedEventData azureSystemEvent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}