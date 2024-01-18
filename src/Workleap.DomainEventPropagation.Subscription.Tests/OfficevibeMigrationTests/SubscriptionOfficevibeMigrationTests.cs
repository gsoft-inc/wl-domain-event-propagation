using System.Text.Json;
using Azure.Messaging.EventGrid;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Officevibe.DomainEvents;

namespace Workleap.DomainEventPropagation.Subscription.Tests.OfficevibeMigrationTests;

public sealed class SubscriptionOfficevibeMigrationTests : BaseIntegrationTest<OfficevibeSubscriptionMigrationFixture>
{
    public SubscriptionOfficevibeMigrationTests(OfficevibeSubscriptionMigrationFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task GivenEventSerializedFromOfficevibe_WhenHandleEventGridWebhookEventAsync_ThenEventHandled()
    {
        var domainEvent = new OfficevibeEvent() { Number = 1, Text = "Hello world" };
        var serializedEvent = JsonSerializer.Serialize(domainEvent);
        var eventGridEvent = new EventGridEvent("subject", domainEvent.GetType().FullName, "1.0", BinaryData.FromString(serializedEvent));

        var webhookHandler = this.Services.GetRequiredService<IDomainEventGridWebhookHandler>();
        await webhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);

        var testState = this.Services.GetRequiredService<SubscriberMigrationTestState>();

        Assert.Equal(1, testState.OfficevibeDomainEventHandlerCallCount);
    }
}

public sealed class OfficevibeSubscriptionMigrationFixture : BaseIntegrationFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddEventPropagationSubscriber()
            .AddDomainEventHandler<OfficevibeEvent, OfficevibeDomainEventHandler>();

        services.AddSingleton<SubscriberMigrationTestState>();

        return services;
    }
}

public class SubscriberMigrationTestState
{
    public int OfficevibeDomainEventHandlerCallCount { get; set; }
}
