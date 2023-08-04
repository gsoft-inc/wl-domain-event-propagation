using Azure.Identity;
using Azure.Messaging.EventGrid;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class ServiceCollectionEventPropagationExtensionsTests
{
    [Fact]
    public void GivenEventPropagationConfigPresent_WhenAddEventPropagationPublisher_ThenOptionsAreSet()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicName"] = "topicName",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var topicProvider = A.Fake<ITopicProvider>();
        A.CallTo(() => topicProvider.GetAllTopicsNames()).Returns(new[] { "topicName" });

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(topicProvider);

        // When
        services.AddEventPropagationPublisher();
        var sp = services.BuildServiceProvider();
        var publisherOptions = sp.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

        // Then
        Assert.Equal("topicName", publisherOptions.TopicName);
        Assert.Equal("http://topicEndpoint.io", publisherOptions.TopicEndpoint);
        Assert.Equal("topicAccessKey", publisherOptions.TopicAccessKey);
    }

    [Fact]
    public void GivenEventPropagationConfigure_WhenAddEventPropagationPublisher_ThenOptionsAreSet()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicName"] = "topicName",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var topicProvider = A.Fake<ITopicProvider>();
        A.CallTo(() => topicProvider.GetAllTopicsNames()).Returns(new[] { "topicName" });

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(topicProvider);

        // When
        services.AddEventPropagationPublisher(opt =>
        {
            opt.TopicEndpoint = "http://ovewrite.io";
        });
        var sp = services.BuildServiceProvider();
        var publisherOptions = sp.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

        // Then
        Assert.Equal("topicName", publisherOptions.TopicName);
        Assert.Equal("http://ovewrite.io", publisherOptions.TopicEndpoint);
        Assert.Equal("topicAccessKey", publisherOptions.TopicAccessKey);
    }

    [Fact]
    public void GivenConfigWithAccessKey_WhenAddEventPropagationPublisher_ThenEventGridPublisherClientConfigured()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicName"] = "topicName",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicAccessKey"] = "topicAccessKey",
        };

        var topicProvider = A.Fake<ITopicProvider>();
        A.CallTo(() => topicProvider.GetAllTopicsNames()).Returns(new[] { "topicName" });

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(topicProvider);

        // When
        services.AddEventPropagationPublisher();
        var sp = services.BuildServiceProvider();
        var clientFactory = sp.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
        var client = clientFactory.CreateClient(EventPropagationPublisherOptions.ClientName);

        // Then
        Assert.NotNull(client);
    }

    [Fact]
    public void GivenConfigWithTokenCredentials_WhenAddEventPropagationPublisher_ThenEventGridPublisherClientConfigured()
    {
        // Given
        var inMemorySettings = new Dictionary<string, string?>
        {
            [$"{EventPropagationPublisherOptions.SectionName}:TopicName"] = "topicName",
            [$"{EventPropagationPublisherOptions.SectionName}:TopicEndpoint"] = "http://topicEndpoint.io",
        };

        var topicProvider = A.Fake<ITopicProvider>();
        A.CallTo(() => topicProvider.GetAllTopicsNames()).Returns(new[] { "topicName" });

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(topicProvider);

        // When
        services.AddEventPropagationPublisher(opt =>
        {
            opt.TokenCredential = new AzureCliCredential();
        });
        var sp = services.BuildServiceProvider();
        var clientFactory = sp.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
        var client = clientFactory.CreateClient(EventPropagationPublisherOptions.ClientName);

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