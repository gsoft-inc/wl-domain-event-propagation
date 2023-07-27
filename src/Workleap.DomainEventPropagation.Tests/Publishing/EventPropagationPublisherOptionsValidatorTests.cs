using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationPublisherOptionsValidatorTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData(" ", "whatever", "http://workleap.com")]
    [InlineData("unrecognizedTopic", "whatever", "http://workleap.com")]
    [InlineData("Signup", null, "http://workleap.com")]
    [InlineData("InvalidTopic", "AccessKey", "http://workleap.com")]
    [InlineData(" ", "AccessKey", "http://workleap.com")]
    [InlineData(null, "AccessKey", "http://workleap.com")]
    [InlineData("Organization", null, "http://workleap.com")]
    public void GivenInvalidEventPropagationConfigurations_WhenValidatingOption_ThenFail(string topicName, string topicAccessKey, string topicEndpoint, bool useTokenCrential = false)
    {
        var topicProviderMock = new Mock<ITopicProvider>();
        topicProviderMock.Setup(x => x.GetAllTopicsNames()).Returns(new[] { "Organization", "Signup" });

        var validator = new EventPropagationPublisherOptionsValidator(new [] { topicProviderMock.Object });

        var result = validator.Validate("Patato", new EventPropagationPublisherOptions
        {
            TokenCredential = useTokenCrential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicName = topicName,
            TopicAccessKey = topicAccessKey
        });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("Signup", " ", "http://workleap.com", true)]
    [InlineData("Signup", "AccessKey", "http://workleap.com", true)]
    [InlineData("Organization", "AccessKey", "http://workleap.com", false)]
    public void GivenValidEventPropagationConfigurations_WhenValidatingOption_ThenSucceeded(string topicName, string topicAccessKey, string topicEndpoint, bool useTokenCrential = false)
    {
        var topicProviderMock = new Mock<ITopicProvider>();
        topicProviderMock.Setup(x => x.GetAllTopicsNames()).Returns(new[] { "Organization", "Signup" });

        var validator = new EventPropagationPublisherOptionsValidator(new [] { topicProviderMock.Object });

        var result = validator.Validate("Patato", new EventPropagationPublisherOptions
        {
            TokenCredential = useTokenCrential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicName = topicName,
            TopicAccessKey = topicAccessKey
        });

        Assert.True(result.Succeeded);
    }
}