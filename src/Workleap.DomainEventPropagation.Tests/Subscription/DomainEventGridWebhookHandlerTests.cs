using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;
using Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class DomainEventGridWebhookHandlerTests
{
    private const string OrganizationTopicName = "Organization";

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsNotSubscribedTo_ThenDomainEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(false);

        //Given 2
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", new TestDomainEvent { Number = 1, Text = "Hello" })
        {
            Topic = "UnregisteredTopic"
        }, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedToButThereIsNoDomainEventHandler_ThenDomainEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        //No eventHandler is registered
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);

        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedTo_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenTopicIsSubscribedToAndThereIsNoConfiguration_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
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
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var domainEventHandler = A.Fake<IDomainEventHandler<TestExceptionDomainEvent>>();
        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestExceptionDomainEvent>._, A<CancellationToken>._)).Throws(new Exception("Test exception"));
        services.AddSingleton<IDomainEventHandler<TestExceptionDomainEvent>>(domainEventHandler);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);

        await Assert.ThrowsAsync<TargetInvocationException>(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", typeof(TestExceptionDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(new TestExceptionDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None));

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestExceptionDomainEvent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenEventCannotBeDeserialized_ThenTelemetryEventIsTracked()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(domainEventHandler);

        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(new EventGridEvent("subject", "SomeNamepsace.OhNo.Hohoa", "version", JsonSerializer.Serialize(new TestDomainEvent { Number = 1, Text = "Hello" }))
        {
            Topic = OrganizationTopicName
        }, CancellationToken.None);
    }
}