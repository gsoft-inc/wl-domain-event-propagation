using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.AzureSystemEvents;
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

        await using var serviceProvider = services.BuildServiceProvider();
        var domainEventGridWebhookHandler = serviceProvider.GetRequiredService<IDomainEventGridWebhookHandler>();

        try
        {
            var eventGridEvent = new EventGridEvent(
                "subject",
                typeof(OneDomainEvent).FullName,
                "version",
                JsonSerializer.Serialize(new OneDomainEvent { Number = 1, Text = "Hello" }))
            {
                Topic = OrganizationTopicName
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
                Topic = OrganizationTopicName
            };

            await domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Equal("HandleDomainEventAsync called for TwoDomainEvent", e.Message);
        }
    }

    [Fact(Skip = "Enable when system topics are used")]
    public async Task GivenAzureSystemEventIsFired_WhenAzureSystemEventHandlerIsRegisteredToMultipleAzureSystemEvents_ThenAzureSystemEventHandlerIsCalled()
    {
        var systemTopicName = "SystemTopicName";
        var systemTopicPattern = "SystemTopicPattern";

        var services = new ServiceCollection();
        var eventProcessingBuilder = services.AddEventPropagationSubscriber(options => { options.SubscribedTopics = new[] { systemTopicName }; });
        eventProcessingBuilder.AddAzureSystemEventHandlersFromAssembly(typeof(AzureSystemEventHandler).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var azureSystemEventGridWebhookHandler = serviceProvider.GetRequiredService<IAzureSystemEventGridWebhookHandler>();

        try
        {
            var eventGridEvent = new EventGridEvent(
                "subject",
                SystemEventNames.MediaJobFinished,
                "version",
                BinaryData.FromString(@"{ ""outputs"": [] }"))
            {
                Topic = $"xzxzxzx{systemTopicPattern}xzxzxzx"
            };

            var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
            if (!wasParsedAsSystemEvent)
            {
                Assert.Fail("Could not deserialize the event data of type 'MediaJobFinishedEventData' as a valid Azure System Event");
            }

            await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Equal("HandleAzureSystemEventAsync called for MediaJobFinishedEventData", e.Message);
        }

        try
        {
            var eventGridEvent = new EventGridEvent(
                "subject2",
                SystemEventNames.MediaJobErrored,
                "version2",
                BinaryData.FromString(@"{ ""outputs"": [] }"))
            {
                Topic = $"xzxzxzx{systemTopicPattern}xzxzxzx"
            };

            var wasParsedAsSystemEvent = eventGridEvent.TryGetSystemEventData(out var systemEventData);
            if (!wasParsedAsSystemEvent)
            {
                Assert.Fail("Could not deserialize the event data of type 'MediaJobErroredEventData' as a valid Azure System Event");
            }

            await azureSystemEventGridWebhookHandler.HandleEventGridWebhookEventAsync(eventGridEvent, systemEventData, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Equal("HandleAzureSystemEventAsync called for MediaJobErroredEventData", e.Message);
        }
    }
}