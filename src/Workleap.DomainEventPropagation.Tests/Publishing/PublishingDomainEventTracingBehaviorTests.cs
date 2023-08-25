using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public sealed class PublishingDomainEventTracingBehaviorTests : BaseUnitTest<TracingFixture>
{
    private readonly InMemoryActivityTracker _activities;
    private readonly IEventPropagationClient _eventPropagationClient;

    public PublishingDomainEventTracingBehaviorTests(TracingFixture fixture, ITestOutputHelper testOutputHelper)
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
}