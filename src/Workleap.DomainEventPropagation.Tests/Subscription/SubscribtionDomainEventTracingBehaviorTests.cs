using Azure.Messaging.EventGrid;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Tests.Publishing;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public sealed class SubscribtionDomainEventTracingBehaviorTests : BaseUnitTest<TracingFixture>
{
    private readonly InMemoryActivityTracker _activities;

    public SubscribtionDomainEventTracingBehaviorTests(TracingFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._activities = this.Services.GetRequiredService<InMemoryActivityTracker>();
    }

    [Fact]
    public async Task GivenActivityListener_WhenHandleEventGridEvent_ThenHandleWithTracing()
    {
        var domainEvent = new EventGridEvent("subject", typeof(SampleDomainEvent).AssemblyQualifiedName, "version", BinaryData.FromObjectAsJson(new SampleDomainEvent()))
        {
            Topic = "TopicName",
        };
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(this.Services);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None);

        this._activities.AssertSubscribeSuccessful();
    }
}