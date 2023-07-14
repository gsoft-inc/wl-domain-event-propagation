using Moq;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

public sealed class TestExceptionDomainEventHandlerMock : Mock<IDomainEventHandler<TestExceptionDomainEvent>>
{
    public TestExceptionDomainEventHandlerMock()
    {
        this.Setup(x => x.HandleDomainEventAsync(It.IsAny<TestExceptionDomainEvent>(), CancellationToken.None)).Throws(new Exception("Test exception"));
    }
}