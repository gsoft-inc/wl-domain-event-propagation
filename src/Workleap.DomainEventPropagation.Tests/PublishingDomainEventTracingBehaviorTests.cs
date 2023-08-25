using Azure.Messaging.EventGrid;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Tests;

public sealed class PublishingDomainEventTracingBehaviorTests : BaseUnitTest<TracingBehaviorFixture>
{
    private readonly InMemoryActivityTracker _activities;
    private readonly IEventPropagationClient _eventPropagationClient;

    public PublishingDomainEventTracingBehaviorTests(TracingBehaviorFixture fixture, ITestOutputHelper testOutputHelper)
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
        var domainEvent = new EventGridEvent("subject", typeof(SampleDomainEvent).AssemblyQualifiedName, "version", BinaryData.FromObjectAsJson(new SampleDomainEvent()))
        {
            Topic = "TopicName",
        };
        var domainEventGridWebhookHandler = new DomainEventGridWebhookHandler(this.Services);
        await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(domainEvent, CancellationToken.None);

        this._activities.AssertSubscribeSuccessful();
    }
}