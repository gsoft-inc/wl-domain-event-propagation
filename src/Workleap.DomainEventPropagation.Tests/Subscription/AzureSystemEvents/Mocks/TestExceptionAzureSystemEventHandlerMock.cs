using Azure.Messaging.EventGrid.SystemEvents;
using Moq;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents.Mocks;

public sealed class TestExceptionAzureSystemEventHandlerMock : Mock<IAzureSystemEventHandler<MediaJobCanceledEventData>>
{
    public TestExceptionAzureSystemEventHandlerMock()
    {
        this.Setup(x => x.HandleAzureSystemEventAsync(It.IsAny<MediaJobCanceledEventData>(), CancellationToken.None)).Throws(new Exception("Test exception"));
    }
}