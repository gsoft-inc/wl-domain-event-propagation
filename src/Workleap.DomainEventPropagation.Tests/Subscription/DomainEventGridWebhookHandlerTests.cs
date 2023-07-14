using Azure.Messaging.EventGrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Workleap.DomainEventPropagation.Extensions;
using Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class DomainEventGridWebhookHandlerTests
{
    private const string OrganizationTopicName = "Organization";
    private readonly Mock<ITelemetryClientProvider> _telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsNotSubscribedTo_ThenDomainEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(false);

        //Given 2
        var domainEventHandler = new TestDomainEventHandlerMock();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler.Object);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", new TestDomainEvent { Number = 1, Text = "Hello" })
        {
            Topic = "UnregisteredTopic"
        }, CancellationToken.None);

        domainEventHandler.Verify(x => x.HandleDomainEventAsync(It.IsAny<TestDomainEvent>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedToButThereIsNoDomainEventHandler_ThenDomainEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        //No eventHandler is registered
        var domainEventHandler = new TestDomainEventHandlerMock();

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);

        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        domainEventHandler.Verify(x => x.HandleDomainEventAsync(It.IsAny<TestDomainEvent>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedTo_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        // Given 2
        var domainEventHandler = new TestDomainEventHandlerMock();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler.Object);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        domainEventHandler.Verify(x => x.HandleDomainEventAsync(It.IsAny<TestDomainEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedToAndThereIsNoConfiguration_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        // Given 2
        var domainEventHandler = new TestDomainEventHandlerMock();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler.Object);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        domainEventHandler.Verify(x => x.HandleDomainEventAsync(It.IsAny<TestDomainEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public void GivenRegisteredDomainEventHandlers_WhenResolvingDomainEventHandlers_ThenDomainEventHandlersAreResolved()
    {
        // Given
        var expectedDomainEventHandlerTypes = typeof(DomainEventGridWebhookHandlerTests)
            .Assembly.GetTypes()
            .Where(p => !p.IsInterface && !p.IsAbstract && p.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();

        // When
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Then
        var unregisteredDomainEventHandlerTypes = new List<Type>();
        var provider = services.BuildServiceProvider();

        foreach (var domainEventHandlerType in expectedDomainEventHandlerTypes)
        {
            try
            {
                provider.GetService(domainEventHandlerType);
            }
            catch (Exception)
            {
                unregisteredDomainEventHandlerTypes.Add(domainEventHandlerType);
            }
        }

        if (unregisteredDomainEventHandlerTypes.Count > 0)
        {
            Assert.Fail($"Some domain event handlers, or their dependencies, were not registered: {string.Join(", ", unregisteredDomainEventHandlerTypes.Select(x => x.FullName))}");
        }
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenExceptionOccurs_ThenExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        // Given 2
        var domainEventHandler = new TestExceptionDomainEventHandlerMock();
        services.AddSingleton<IDomainEventHandler<TestExceptionDomainEvent>>(domainEventHandler.Object);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);

        await Assert.ThrowsAsync<Exception>(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestExceptionDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestExceptionDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None));

        domainEventHandler.Verify(x => x.HandleDomainEventAsync(It.IsAny<TestExceptionDomainEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenEventCannotBeDeserialized_ThenTelemetryEventIsTracked()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        // Given 2
        var domainEventHandler = new TestDomainEventHandlerMock();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler.Object);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", "SomeNamepsace.OhNo.Hohoa", "version", JsonConvert.SerializeObject((object?)new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        // "Domain event received. Cannot deserialize object"
        _telemetryClientProviderMock.Verify(x => x.TrackEvent(TelemetryConstants.DomainEventDeserializationFailed, It.IsAny<string>(), "SomeNamepsace.OhNo.Hohoa", It.IsAny<TelemetrySpan>()));
    }
}