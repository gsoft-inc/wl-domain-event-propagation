using System.Reflection;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Workleap.DomainEventPropagation.AzureSystemEvents;
using Workleap.DomainEventPropagation.Extensions;
using Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription.AzureSystemEvents;

public class AzureSystemEventGridWebhookHandlerTests
{
    private readonly ITelemetryClientProvider _telemetryClientProvider = A.Fake<ITelemetryClientProvider>();

    [Fact]
    public async Task GivenAzureSystemEventIsFired_WhenTopicIsNotSubscribedTo_ThenAzureSystemEventIsIgnored()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<EventGridEvent>._)).Returns(false);

        //Given 2
        var azureSystemEventHandler = A.Fake<IAzureSystemEventHandler<MediaJobFinishedEventData>>();
        services.AddSingleton<IAzureSystemEventHandler<MediaJobFinishedEventData>>(azureSystemEventHandler);

        var azureSystemEventGridWebhookHandler = new AzureSystemEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator, _telemetryClientProvider);
        var eventGridEvent = new EventGridEvent("subject", SystemEventNames.MediaJobFinished, "version", BinaryData.FromString(@"{ ""outputs"": [] }"))
        {
            Topic = "UnregisteredTopic"
        };

        var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
        if (!wasParsedAsSystemEvent)
        {
            Assert.Fail("Could not deserialize the event data of type 'MediaJobFinishedEventData' as a valid Azure System Event");
        }

        await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None);

        A.CallTo(() => azureSystemEventHandler.HandleAzureSystemEventAsync(A<MediaJobFinishedEventData>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact(Skip = "This is failing, we'll investigate later")]
    public async Task GivenAzureSystemEventIsFired_WhenTopicIsSubscribedToButThereIsNoAzureSystemEventHandler_ThenAzureSystemEventIsIgnored()
    {
        var systemTopicPattern = "SystemTopicPattern";

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        //Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        //No eventHandler is registered
        var azureSystemEventHandler = A.Fake<IAzureSystemEventHandler<MediaJobFinishedEventData>>();

        var azureSystemEventGridWebhookHandler = new AzureSystemEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator, _telemetryClientProvider);
        var eventGridEvent = new EventGridEvent("subject", SystemEventNames.MediaJobFinished, "version", BinaryData.FromString(@"{ ""outputs"": [] }"))
        {
            Topic = $"xzxzxzx{systemTopicPattern}xzxzxzx"
        };

        var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
        if (!wasParsedAsSystemEvent)
        {
            Assert.Fail("Could not deserialize the event data of type 'MediaJobFinishedEventData' as a valid Azure System Event");
        }

        await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None);

        A.CallTo(() => azureSystemEventHandler.HandleAzureSystemEventAsync(A<MediaJobFinishedEventData>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventNoHandlerFound, A<string>._, eventGridEvent.EventType, A<TelemetrySpan>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenAzureSystemEventIsFired_WhenTopicIsSubscribedTo_ThenAzureSystemEventHandlerIsCalled()
    {
        var systemTopicPattern = "SystemTopicPattern";

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var azureSystemEventHandler = A.Fake<IAzureSystemEventHandler<MediaJobFinishedEventData>>();
        services.AddSingleton<IAzureSystemEventHandler<MediaJobFinishedEventData>>(azureSystemEventHandler);

        var azureSystemEventGridWebhookHandler = new AzureSystemEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator, _telemetryClientProvider);
        var eventGridEvent = new EventGridEvent("subject", SystemEventNames.MediaJobFinished, "version", BinaryData.FromString(@"{ ""outputs"": [] }"))
        {
            Topic = $"xzxzxzx{systemTopicPattern}xzxzxzx"
        };

        var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
        if (!wasParsedAsSystemEvent)
        {
            Assert.Fail("Could not deserialize the event data of type 'MediaJobFinishedEventData' as a valid Azure System Event");
        }

        await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None);

        A.CallTo(() => azureSystemEventHandler.HandleAzureSystemEventAsync(A<MediaJobFinishedEventData>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenRegisteredAzureSystemEventHandlers_WhenResolvingAzureSystemEventHandlers_ThenAzureSystemEventHandlersAreResolved()
    {
        // Given
        var expectedAzureSystemEventHandlerTypes = typeof(AzureSystemEventGridWebhookHandlerTests)
            .Assembly.GetTypes()
            .Where(p => !p.IsInterface && !p.IsAbstract && p.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAzureSystemEventHandler<>)))
            .ToList();

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();

        // When
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        // Then
        var unregisteredAzureSystemEventHandlerTypes = new List<Type>();
        var provider = services.BuildServiceProvider();

        foreach (var azureSystemEventHandlerType in expectedAzureSystemEventHandlerTypes)
        {
            try
            {
                provider.GetService(azureSystemEventHandlerType);
            }
            catch (Exception)
            {
                unregisteredAzureSystemEventHandlerTypes.Add(azureSystemEventHandlerType);
            }
        }

        if (unregisteredAzureSystemEventHandlerTypes.Count > 0)
        {
            Assert.Fail($"Some Azure System event handlers, or their dependencies, were not registered: {string.Join(", ", unregisteredAzureSystemEventHandlerTypes.Select(x => x.FullName))}");
        }
    }

    [Fact]
    public async Task GivenAzureSystemEventIsFired_WhenExceptionOccurs_ThenExceptionIsThrown()
    {
        var systemTopicPattern = "SystemTopicPattern";

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var azureSystemEventHandler = A.Fake<IAzureSystemEventHandler<MediaJobCanceledEventData>>();
        A.CallTo(() => azureSystemEventHandler.HandleAzureSystemEventAsync(A<MediaJobCanceledEventData>._, A<CancellationToken>._)).Throws(new Exception("Test exception"));
        services.AddSingleton<IAzureSystemEventHandler<MediaJobCanceledEventData>>(azureSystemEventHandler);

        var azureSystemEventGridWebhookHandler = new AzureSystemEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator, _telemetryClientProvider);
        var eventGridEvent = new EventGridEvent("subject", SystemEventNames.MediaJobCanceled, "version", BinaryData.FromString(@"{ ""outputs"": [] }"))
        {
            Topic = $"xzxzxzx{systemTopicPattern}xzxzxzx"
        };

        var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
        if (!wasParsedAsSystemEvent)
        {
            Assert.Fail("Could not deserialize the event data of type 'MediaJobCanceledEventData' as a valid Azure System Event");
        }

        await Assert.ThrowsAsync<TargetInvocationException>(() => azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None));

        A.CallTo(() => azureSystemEventHandler.HandleAzureSystemEventAsync(A<MediaJobCanceledEventData>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenAzureSystemEventIsFired_WhenEventCannotBeDeserialized_ThenTelemetryEventIsTracked()
    {
        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber();
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventGridWebhookHandlerTests).Assembly);

        // Given 1
        var subscriptionTopicValidator = A.Fake<ISubscriptionTopicValidator>();
        A.CallTo(() => subscriptionTopicValidator.IsSubscribedToTopic(A<string>._)).Returns(true);

        // Given 2
        var azureSystemEventHandler = new TestAzureSystemEventHandlerMock();
        services.AddSingleton<IAzureSystemEventHandler<MediaJobFinishedEventData>>(azureSystemEventHandler.Object);

        var azureSystemEventGridWebhookHandler = new AzureSystemEventGridWebhookHandler(services.BuildServiceProvider(), subscriptionTopicValidator, _telemetryClientProvider);
        var eventGridEvent = new EventGridEvent("subject", SystemEventNames.RedisPatchingCompleted, "version", BinaryData.FromString(@"{ ""name"": ""name"", ""timestamp"": ""timestamp"", ""status"": ""status"" }"))
        {
            Topic = $"SomeRedisTopic"
        };

        eventGridEvent.TryGetSystemEventData(out var systemEventData);
        await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None);

        // "Azure System event received. Cannot deserialize object"
        A.CallTo(() => _telemetryClientProvider.TrackEvent(TelemetryConstants.AzureSystemEventDeserializationFailed, A<string>._, SystemEventNames.RedisPatchingCompleted, A<TelemetrySpan>._)).MustHaveHappenedOnceExactly();
    }
}