using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class ServiceCollectionEventSubscriptionExtensionsTests
{
    [Fact]
    public void GivenIConfiguration_WhenAddSubscriber_ThenOptionsAreRegistered()
    {
        // Given
        var myConfiguration = new Dictionary<string, string>
        {
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicAccessKey)}"] = "accessKey",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicEndpoint)}"] = "http://topicurl.com",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.SubscriptionName)}"] = "sub-name",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicName)}"] = "topic-name",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationSubscriber();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventPropagationSubscriptionOptions>>().Value;

        // Then
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicEndpoint)}"], options.TopicEndpoint);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicAccessKey)}"], options.TopicAccessKey);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.SubscriptionName)}"], options.SubscriptionName);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicName)}"], options.TopicName);
    }

    [Fact]
    public void GivenIConfiguration_WhenAddSubscriber_CanOverrideConfiguration()
    {
        // Given
        var myConfiguration = new Dictionary<string, string>
        {
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicAccessKey)}"] = "accessKey",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicEndpoint)}"] = "http://topicurl.com",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.SubscriptionName)}"] = "sub-name",
            [$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicName)}"] = "topic-name",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationSubscriber(options => { options.TopicEndpoint = "http://ovewrite.io"; });
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventPropagationSubscriptionOptions>>().Value;

        // Then
        Assert.Equal("http://ovewrite.io", options.TopicEndpoint);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicAccessKey)}"], options.TopicAccessKey);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.SubscriptionName)}"], options.SubscriptionName);
        Assert.Equal(myConfiguration[$"{EventPropagationSubscriptionOptions.SectionName}:{nameof(EventPropagationSubscriptionOptions.TopicName)}"], options.TopicName);
    }
}