using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;
using Workleap.DomainEventPropagation.Tests.Subscription.Models;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class RegistrationTest
{
    private const string OrganizationTopicName = "Organization";

    [Fact]
    public async Task GivenDomainEventIsFired_WhenDomainEventHandlerIsRegisteredToMultipleDomainEvents_ThenDomainEventHandlerIsCalled()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"{EventPropagationSubscriberOptions.SectionName}:SubscribedTopics:0", OrganizationTopicName },
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddEventPropagationSubscriber()
            .AddDomainEventHandlersFromAssembly(typeof(DomainEventHandler).Assembly);

        using var serviceProvider = services.BuildServiceProvider();
        var domainEventGridWebhookHandler = serviceProvider.GetRequiredService<IDomainEventGridWebhookHandler>();

        try
        {
            var eventGridEvent = new EventGridEvent(
                "subject",
                typeof(OneDomainEvent).FullName,
                "version",
                JsonSerializer.Serialize(new OneDomainEvent { Number = 1, Text = "Hello" }))
            {
                Topic = OrganizationTopicName,
            };

            await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Equal("HandleDomainEventAsync called for OneDomainEvent", e.Message);
        }

        try
        {
            var eventGridEvent = new EventGridEvent(
                "subject2",
                typeof(TwoDomainEvent).FullName,
                "version2",
                JsonSerializer.Serialize(new TwoDomainEvent { Number = 1, Text = "Hello" }))
            {
                Topic = OrganizationTopicName,
            };

            await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Equal("HandleDomainEventAsync called for TwoDomainEvent", e.Message);
        }
    }
}