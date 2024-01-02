using Azure.Identity;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class ServiceCollectionEventPropagationExtensionsTests
{
    [Fact]
    public void GivenEventPropagationConfigPresent_WhenAddEventPropagationPublisher_ThenOptionsAreSet()
    {

        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationPublisher();
        var serviceProvider = services.BuildServiceProvider();
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

        // Then
        Assert.Equal("http://topicEndpoint.io", publisherOptions.TopicEndpoint);
        Assert.Equal("topicAccessKey", publisherOptions.TopicAccessKey);
    }

    [Fact]
    public void GivenEventPropagationConfigure_WhenAddEventPropagationPublisher_ThenOptionsAreSet()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationPublisher(options =>
        {
            options.TopicEndpoint = "http://ovewrite.io";
        });
        var serviceProvider = services.BuildServiceProvider();
        var publisherOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

        // Then
        Assert.Equal("http://ovewrite.io", publisherOptions.TopicEndpoint);
        Assert.Equal("topicAccessKey", publisherOptions.TopicAccessKey);
    }

    [Fact]
    public void GivenConfigWithAccessKey_WhenAddEventPropagationPublisher_ThenEventGridPublisherClientConfigured()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationPublisher();
        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
        var client = clientFactory.CreateClient(EventPropagationPublisherOptions.CustomTopicClientName);

        // Then
        Assert.NotNull(client);
    }

    [Fact]
    public void GivenConfigWithTokenCredentials_WhenAddEventPropagationPublisher_ThenEventGridPublisherClientConfigured()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);

        // When
        services.AddEventPropagationPublisher(options =>
        {
            options.TokenCredential = new AzureCliCredential();
        });
        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
        var client = clientFactory.CreateClient(EventPropagationPublisherOptions.CustomTopicClientName);

        // Then
        Assert.NotNull(client);
    }

    [Fact]
    public void GivenNullServiceCollection_WhenAddEventPropagationPublisher_ThenThrowsArgumentNullException()
    {
        // Given
        var services = (IServiceCollection?)null;

        // When
        var exception = Assert.Throws<ArgumentNullException>(() => services!.AddEventPropagationPublisher());

        // Then
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void GivenNullConfigure_WhenAddEventPropagationPublisher_ThenThrowsArgumentNullException()
    {
        // Given
        var services = new ServiceCollection();
        Action<EventPropagationPublisherOptions>? configure = null;

        // When
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddEventPropagationPublisher(configure!));

        // Then
        Assert.Equal("configure", exception.ParamName);
    }
}