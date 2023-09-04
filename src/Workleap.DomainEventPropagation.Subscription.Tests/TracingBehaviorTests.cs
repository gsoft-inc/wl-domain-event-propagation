using Azure.Messaging.EventGrid;
using FakeItEasy;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Workleap.DomainEventPropagation.Tests;

namespace Workleap.DomainEventPropagation.Subscription.Tests;

public sealed class TracingBehaviorTests : BaseUnitTest<TracingBehaviorFixture>
{
    private readonly InMemoryActivityTracker _activities;

    public TracingBehaviorTests(TracingBehaviorFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._activities = this.Services.GetRequiredService<InMemoryActivityTracker>();
    }

    [Fact]
    public async Task GivenActivityListener_WhenHandleEventGridEvent_ThenHandleWithTracing()
    {
        var wrapperEvent = DomainEventWrapper.Wrap(new SampleDomainEvent { Message = "Hello world" });

        var eventGridEvent = new EventGridEvent("subject", wrapperEvent.GetType().FullName, "version", BinaryData.FromObjectAsJson(wrapperEvent))
        {
            Topic = "TopicName",
        };

        var behaviors = this.Services.GetServices<ISubscriptionDomainEventBehavior>();
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(this.Services, A.Fake<IDomainEventTypeRegistry>(), NullLogger<DomainEventGridWebhookHandler>.Instance, behaviors);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);

        this._activities.AssertSubscribeSuccessful();
    }

    [Fact]
    public async Task GivenTracingBehaviors_WhenRegisterBehaviors_ThenRegisteredInRightOrder()
    {
        var subscriptionBehaviors = this.Services.GetServices<ISubscriptionDomainEventBehavior>().ToArray();

        Assert.IsType<TracingSubscriptionDomainEventBehavior>(subscriptionBehaviors[0]);
        Assert.IsType<ApplicationInsightsSubscriptionDomainEventBehavior>(subscriptionBehaviors[1]);
    }
}