using Moq;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

public sealed class TestDomainEventHandlerMock : Mock<IDomainEventHandler<TestDomainEvent>>
{
    public TestDomainEventHandlerMock()
    {
        this.Setup(x => x.HandleDomainEventAsync(It.IsAny<TestDomainEvent>(), CancellationToken.None));
    }
}