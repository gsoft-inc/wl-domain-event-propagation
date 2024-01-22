using Azure.Messaging.EventGrid;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Officevibe.DomainEvents;

namespace Workleap.DomainEventPropagation.Subscription.Tests;

public sealed class DomainEventGridWebhookHandlerIntegrationTests :
    BaseIntegrationTest<DomainEventGridWebhookHandlerIntegrationTests.OfficevibeSubscriptionMigrationFixture>
{
    public DomainEventGridWebhookHandlerIntegrationTests(OfficevibeSubscriptionMigrationFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task GivenDomainEvent_WhenHandleEventGridWebhookEventAsync_ThenEventHandled()
    {
        // Given
        var domainEvent = new TestDomainEvent() { Message = "Hello world" };
        var domainEventWrapper = DomainEventWrapper.Wrap(domainEvent);

        // Serializing the same way the Officevibe library does
        var eventGridEvent = new EventGridEvent(
            domainEventWrapper.DomainEventName,
            domainEventWrapper.DomainEventName,
            "1.0",
            new BinaryData(domainEventWrapper.Data));

        var webhookHandler = this.Services.GetRequiredService<IDomainEventGridWebhookHandler>();

        // When
        await webhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);
        var testState = this.Services.GetRequiredService<DomainEventGridWebhookHandlerTestState>();

        // Then
        Assert.Equal(1, testState.OfficevibeDomainEventHandlerCallCount);
    }

    [Fact]
    public async Task GivenEventSerializedFromOfficevibe_WhenHandleEventGridWebhookEventAsync_ThenEventHandled()
    {
        // Given
        var domainEvent = new OfficevibeEvent() { Number = 1, Text = "Hello world" };

        // Serializing the same way the Officevibe library does
        var serializedEvent = JsonConvert.SerializeObject(domainEvent);
        var eventGridEvent = new EventGridEvent("subject", domainEvent.GetType().FullName, "1.0", BinaryData.FromString(serializedEvent));

        var webhookHandler = this.Services.GetRequiredService<IDomainEventGridWebhookHandler>();

        // When
        await webhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);
        var testState = this.Services.GetRequiredService<DomainEventGridWebhookHandlerTestState>();

        // Then
        Assert.Equal(1, testState.OfficevibeDomainEventHandlerCallCount);
    }

    public sealed class OfficevibeSubscriptionMigrationFixture : BaseIntegrationFixture
    {
        public override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddEventPropagationSubscriber()
                .AddDomainEventHandler<OfficevibeEvent, OfficevibeDomainEventHandler>()
                .AddDomainEventHandler<TestDomainEvent, TestDomainEventHandler>();

            services.AddSingleton<DomainEventGridWebhookHandlerTestState>();

            return services;
        }
    }

    [DomainEvent("test-event")]
    private class TestDomainEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }

    private class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
    {
        private readonly DomainEventGridWebhookHandlerTestState _testState;

        public TestDomainEventHandler(DomainEventGridWebhookHandlerTestState testState)
        {
            this._testState = testState;
        }

        public Task HandleDomainEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            this._testState.OfficevibeDomainEventHandlerCallCount++;
            return Task.CompletedTask;
        }
    }

    public class DomainEventGridWebhookHandlerTestState
    {
        public int OfficevibeDomainEventHandlerCallCount { get; set; }
    }
}