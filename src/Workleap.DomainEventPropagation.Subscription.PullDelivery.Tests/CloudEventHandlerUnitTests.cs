using System.Reflection;
using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests.Events;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class CloudEventHandlerUnitTests
{
    private const string SampleCloudEventTypeName = "sample-cloud-event";

    [Fact]
    public async Task Given_IllFormedEvent_When_HandleCloudEventAsync_Then_ThrowCloudEventSerializationException()
    {
        // Given
        var cloudEvent = new CloudEvent(
            type: SampleCloudEventTypeName,
            source: "http://source.com",
            data: BinaryData.FromString("not a json"),
            dataContentType: typeof(SampleEvent).FullName);

        var services = new ServiceCollection();
        services
            .AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandler<SampleEvent, SampleEventTestHandler>();
        var handler = GivenCloudEventHandler(services);

        // When
        await Assert.ThrowsAsync<CloudEventSerializationException>(() => handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Given_EventTypeNotRegistered_When_HandleCloudEventAsync_Then_ThrowEventDomainTypeNotRegisteredException()
    {
        // Given
        var cloudEvent = GivenSampleEvent();
        var services = new ServiceCollection();
        services.AddPullDeliverySubscription();
        var handler = GivenCloudEventHandler(services);

        // When
        await Assert.ThrowsAsync<EventDomainTypeNotRegisteredException>(() => handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Given_TypeWasRegisteredButNoHandler_When_HandleCloudEventAsync_Then_ReturnsRejected()
    {
        // Given
        var cloudEvent = GivenSampleEvent();
        var services = new ServiceCollection();
        services.AddPullDeliverySubscription();
        var domainEventTypeRegistry = new DomainEventTypeRegistry();
        domainEventTypeRegistry.RegisterDomainEvent(typeof(SampleEvent));
        services.Replace(new ServiceDescriptor(typeof(IDomainEventTypeRegistry), domainEventTypeRegistry));
        var handler = new CloudEventHandler(new ServiceCollection().BuildServiceProvider(), domainEventTypeRegistry, Enumerable.Empty<ISubscriptionDomainEventBehavior>(), new NullLogger<CloudEventHandler>());

        // When
        await Assert.ThrowsAsync<EventDomainHandlerNotRegistered>(() => handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Given_FailingEventHandler_When_HandleCloudEventAsync_Then_ThrowException()
    {
        // Given
        var wrapper = DomainEventWrapper.Wrap(new SampleThatCausesExceptionDomainEvent() { Message = "A message" });
        var cloudEvent = new CloudEvent(
            type: wrapper.DomainEventName,
            source: "http://source.com",
            jsonSerializableData: wrapper.Data);
        var services = new ServiceCollection();
        services
            .AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandler<SampleThatCausesExceptionDomainEvent, SampleThatCausesExceptionDomainEventHandler>();
        var handler = GivenCloudEventHandler(services);

        // When
        await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None));
    }

    [Fact]
    public async Task Given_EventHandler_When_HandleCloudEventAsync_Then_EventIsTreated()
    {
        // Given
        const string eventMessage = "A super important message!";
        var cloudEvent = GivenSampleEvent(eventMessage);
        var services = new ServiceCollection();
        services
            .AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandler<SampleEvent, SampleEventTestHandler>();
        var handler = GivenCloudEventHandler(services);

        // When
        await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        Assert.Single(SampleEventTestHandler.ReceivedEvents, e => e.Message == eventMessage);
    }

    [Fact]
    public async Task Given_EventHandlersFromAssembly_When_HandleCloudEventAsync_Then_EventIsTreated()
    {
        // Given
        const string eventMessage = "Another super important message!";
        var cloudEvent = GivenSampleEvent(eventMessage);
        var services = new ServiceCollection();
        services
            .AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandlers(Assembly.GetAssembly(typeof(CloudEventHandlerUnitTests))!);
        var handler = GivenCloudEventHandler(services);

        // When
        await handler.HandleCloudEventAsync(cloudEvent, CancellationToken.None);

        // Then
        Assert.Single(SampleEventTestHandler.ReceivedEvents, e => e.Message == eventMessage);
    }

    private static CloudEvent GivenSampleEvent(string message = "Hello World!")
    {
        var wrapper = DomainEventWrapper.Wrap(new SampleEvent() { Message = message });
        var cloudEvent = new CloudEvent(
            type: wrapper.DomainEventName,
            source: "http://source.com",
            jsonSerializableData: wrapper.Data);
        return cloudEvent;
    }

    private static ICloudEventHandler GivenCloudEventHandler(IServiceCollection services)
    {
        services.AddSingleton<ILogger<ICloudEventHandler>, NullLogger<ICloudEventHandler>>();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ICloudEventHandler>();
    }
}