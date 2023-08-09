using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationPublisherOptionsTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData(" ", "accessKey", "http://topicurl.com")]
    [InlineData(null, "accessKey", "http://topicurl.com")]
    [InlineData("topicName", " ", "http://topicurl.com")]
    [InlineData("topicName", null, "http://topicurl.com")]
    [InlineData("topicName", "accessKey", " ")]
    [InlineData("topicName", "accessKey", null)]
    [InlineData("topicName", "accessKey", "topicEndpoint")]
    public void GivenEventPropagationConfiguration_WhenOptionsAreInvalid_ThrowsException(string topicName, string topicAccessKey, string topicEndpoint)
    {
        var myConfiguration = new Dictionary<string, string>
        {
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}", topicName },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}", topicEndpoint },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}", topicAccessKey },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddEventPropagationPublisherOptions(_ => { });

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value);
    }

    [Fact]
    public void GivenEventPropagationConfigurationAsIConfiguration_WhenLoadedProperly_ThenPropertiesMatch()
    {
        var myConfiguration = new Dictionary<string, string>
        {
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}", "topicName" },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}", "accessKey" },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}", "http://topicurl.com" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddEventPropagationPublisherOptions(_ => { });

        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<EventPropagationPublisherOptions>>().Value;

        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}"], options.TopicName);
        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}"], options.TopicEndpoint);
        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}"], options.TopicAccessKey);
    }
}