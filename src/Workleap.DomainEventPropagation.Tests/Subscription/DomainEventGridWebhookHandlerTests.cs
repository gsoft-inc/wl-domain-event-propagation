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
    private const string TopicName = "TopicName";

    private static readonly DomainEventWrapper DomainEvent = new()
    {
        DomainEventJson = JsonSerializer.SerializeToElement(new TestDomainEvent() { Number = 1, Text = "Hello world" }),
        DomainEventType = typeof(TestDomainEvent).AssemblyQualifiedName ?? typeof(TestDomainEvent).ToString(),
    };

    [Fact]
    public async Task GivenDomainEventIsFired_WhenThereIsNoDomainEventHandler_ThenDomainEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // No eventHandler is registered
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();

        var domainEvent = new EventGridEvent("subject", typeof(TestDomainEvent).AssemblyQualifiedName, "version", BinaryData.FromObjectAsJson(DomainEvent))
        {
            Topic = TopicName,
        };
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), Array.Empty<ISubscriptionDomainEventBehavior>());
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenThereIsADomainEventHandler_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        services.AddSingleton(domainEventHandler);

        var domainEvent = new EventGridEvent("subject", typeof(TestDomainEvent).AssemblyQualifiedName, "version", BinaryData.FromObjectAsJson(DomainEvent))
        {
            Topic = TopicName,
        };

        var serviceProvider = services.BuildServiceProvider();
        var behaviors = serviceProvider.GetServices<ISubscriptionDomainEventBehavior>();
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(serviceProvider, behaviors);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None);

        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenDomainEventTypeNotFound_ThenException()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // No eventHandler is registered
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();

        var domainEvent = new EventGridEvent("subject", typeof(TestDomainEvent).FullName, "version", BinaryData.FromObjectAsJson(DomainEvent))
        {
            Topic = TopicName,
        };
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(services.BuildServiceProvider(), Array.Empty<ISubscriptionDomainEventBehavior>());

        await Assert.ThrowsAsync<TypeLoadException>(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None));
        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenDomainEventIsFired_WhenExceptionOccurs_ThenExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddDomainEventHandlersFromAssembly(typeof(DomainEventGridWebhookHandlerTests).Assembly);

        // Given
        var domainEventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();
        A.CallTo(() => domainEventHandler.HandleDomainEventAsync(A<TestDomainEvent>._, A<CancellationToken>._)).Throws(new Exception("Test exception"));
        services.AddSingleton(domainEventHandler);

        var serviceProvider = services.BuildServiceProvider();
        var behaviors = serviceProvider.GetServices<ISubscriptionDomainEventBehavior>();
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(serviceProvider, behaviors);

        await Assert.ThrowsAsync<TargetInvocationException>(() =>
        {
            var domainEvent = new EventGridEvent("subject", typeof(TestDomainEvent).AssemblyQualifiedName, "version", BinaryData.FromObjectAsJson(DomainEvent))
            {
                Topic = TopicName,
            };
            return domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None);
        });

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
    public async Task GivenRegisteredTracingBehavior_WhenHandleEventGridWebhookEventAsync_ThenBehaviorCalled()
    {
        // Given
        var eventGridEvent = new EventGridEvent("Subject", typeof(TestDomainEvent).AssemblyQualifiedName, "1.0", new BinaryData(DomainEvent));

        var subscriberBehavior = A.Fake<ISubscriptionDomainEventBehavior>();
        var eventHandler = A.Fake<IDomainEventHandler<TestDomainEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton(subscriberBehavior);
        services.AddSingleton(eventHandler);
        var serviceProvider = services.BuildServiceProvider();

        // When
        var webhookHandler = new DomainEventGridWebhookHandler(serviceProvider, new[] { subscriberBehavior });

        await webhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);

        // Then
        A.CallTo(() => subscriberBehavior.Handle(A<IDomainEvent>._, A<SubscriberDomainEventsHandlerDelegate>._, A<CancellationToken>._)).MustHaveHappened();
    }
}