using System.Text.Json;
using Azure.Messaging.EventGrid;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Tests;

public sealed class TracingBehaviorTests : BaseUnitTest<TracingBehaviorFixture>
{
    private readonly InMemoryActivityTracker _activities;
    private readonly IEventPropagationClient _eventPropagationClient;

    public TracingBehaviorTests(TracingBehaviorFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._eventPropagationClient = this.Services.GetRequiredService<IEventPropagationClient>();
        this._activities = this.Services.GetRequiredService<InMemoryActivityTracker>();
    }

    [Fact]
    public async Task GivenActivityListener_WhenPublishDomainEvent_ThenHandleWithTracing()
    {
        var domainEvent = new SampleDomainEvent();
        await this._eventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        this._activities.AssertPublishSuccessful();
    }

    [Fact]
    public async Task GivenActivityListener_WhenHandleEventGridEvent_ThenHandleWithTracing()
    {
        var wrapperEvent = new DomainEventWrapper
        {
            DomainEventJson = JsonSerializer.SerializeToElement(new SampleDomainEvent { Message = "Hello world" }),
            DomainEventType = typeof(SampleDomainEvent).AssemblyQualifiedName ?? typeof(SampleDomainEvent).ToString(),
        };

        var eventGridEvent = new EventGridEvent("subject", wrapperEvent.GetType().FullName, "version", BinaryData.FromObjectAsJson(wrapperEvent))
        {
            Topic = "TopicName",
        };

        var behaviors = this.Services.GetServices<ISubscriptionDomainEventBehavior>();
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(this.Services, behaviors);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);

        this._activities.AssertSubscribeSuccessful();
    }

    [Fact]
    public async Task GivenTracingBehaviors_WhenRegisterBehaviors_ThenRegisteredInRightOrder()
    {
        var publishingBehaviors = this.Services.GetServices<IPublishingDomainEventBehavior>().ToArray();
        var subscriptionBehaviors = this.Services.GetServices<ISubscriptionDomainEventBehavior>().ToArray();

        Assert.IsType<TracingPublishingDomainEventBehavior>(publishingBehaviors[0]);
        Assert.IsType<ApplicationInsightsPublishingDomainEventBehavior>(publishingBehaviors[1]);

        Assert.IsType<SubscriptionDomainEventTracingBehavior>(subscriptionBehaviors[0]);
        Assert.IsType<SubscriptionApplicationInsightsTracingBehavior>(subscriptionBehaviors[1]);
    }
}