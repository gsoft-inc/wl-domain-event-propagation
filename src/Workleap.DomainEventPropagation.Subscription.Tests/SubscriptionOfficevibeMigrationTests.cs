using System.Text.Json;
using Azure.Messaging.EventGrid;
using FakeItEasy;
using GSoft.Extensions.DependencyInjection;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Officevibe.DomainEvents;

namespace Workleap.DomainEventPropagation.Subscription.Tests;

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
        var eventHandler = this.Services.GetRequiredService<IDomainEventHandler<OfficevibeEvent>>();
        await webhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None);

        A.CallTo(() => eventHandler.HandleDomainEventAsync(A<OfficevibeEvent>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}

public sealed class OfficevibeSubscriptionMigrationFixture : BaseIntegrationFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddEventPropagationSubscriber()
            .AddDomainEventHandler<OfficevibeEvent, OfficevibeDomainEventHandler>();

        services.DecorateWithSameLifetime<IDomainEventHandler<OfficevibeEvent>>(handler =>
        {
            return A.Fake<IDomainEventHandler<OfficevibeEvent>>(x => x.Wrapping(handler));
        });

        return services;
    }
}
