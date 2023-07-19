using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationPublisherOptionsTests
{
    private static readonly Mock<ITopicProvider> TopicProviderMock = new ();

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(" ", "whatever", "http://workleap.com")]
    [InlineData("unrecognizedTopic", "whatever", "http://workleap.com")]
    [InlineData("Signup", null, "http://workleap.com")]
    [InlineData("Signup", "whatever", null)]
    [InlineData("InvalidTopic", "AccessKey", "http://workleap.com")]
    [InlineData(" ", "AccessKey", "http://workleap.com")]
    [InlineData(null, "AccessKey", "http://workleap.com")]
    [InlineData("Organization", "AccessKey", null)]
    [InlineData("Organization", "AccessKey", "")]
    [InlineData("Organization", "AccessKey", "AAAAAAABBBBBCCCCCCDEEEEE")]
    [InlineData("Organization", "  ", "AAAAAAABBBBBCCCCCCDEEEEE")]
    [InlineData("Organization", null, "http://workleap.com")]
    public void GivenEventPropagationConfiguration_WhenOptionsAreInvalid_ThrowsException(string topicName, string topicAccessKey, string topicEndpoint)
    {
        var myConfiguration = new Dictionary<string, string>
        {
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}", topicName },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}", topicEndpoint },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}", topicAccessKey }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        var services = new ServiceCollection();

        TopicProviderMock.Setup(x => x.GetAllTopicsNames()).Returns(new[] { "Organization", "Signup" });
        services.AddSingleton(TopicProviderMock.Object);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEventPropagationPublisherOptions(_ => { });

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => serviceProvider.GetService<IOptions<EventPropagationPublisherOptions>>().Value);
    }

    [Fact]
    public void GivenEventPropagationConfigurationAsIConfiguration_WhenLoadedProperly_ThenPropertiesMatch()
    {
        var myConfiguration = new Dictionary<string, string>
        {
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}", "Organization" },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}", "http://workleap.com" },
            { $"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}", "AccessKey" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        var services = new ServiceCollection();

        TopicProviderMock.Setup(x => x.GetAllTopicsNames()).Returns(new[] { "Organization", "Signup" });
        services.AddSingleton(TopicProviderMock.Object);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEventPropagationPublisherOptions(_ => { });

        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetService<IOptions<EventPropagationPublisherOptions>>().Value;

        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicName)}"], options.TopicName);
        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicEndpoint)}"], options.TopicEndpoint);
        Assert.Equal(myConfiguration[$"{EventPropagationPublisherOptions.SectionName}:{nameof(EventPropagationPublisherOptions.TopicAccessKey)}"], options.TopicAccessKey);
    }
}