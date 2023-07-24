using Azure.Messaging.EventGrid.SystemEvents;
using Moq;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents.Mocks;

public sealed class TestAzureSystemEventHandlerMock : Mock<IAzureSystemEventHandler<MediaJobFinishedEventData>>
{
    public TestAzureSystemEventHandlerMock()
    {
        this.Setup(x => x.HandleAzureSystemEventAsync(It.IsAny<MediaJobFinishedEventData>(), CancellationToken.None));
    }
}