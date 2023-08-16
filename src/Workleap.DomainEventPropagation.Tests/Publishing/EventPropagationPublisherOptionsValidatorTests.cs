using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Publishing;

public class EventPropagationPublisherOptionsValidatorTests
{
    [Theory]
    [InlineData("topicName", " ", "http://topicurl.com", true, true)]
    [InlineData("topicName", " ", "http://topicurl.com", false, false)]
    [InlineData("topicName", "accessKey", "http://topicurl.com", true, true)]
    [InlineData("topicName", "accessKey", "http://topicurl.com", false, true)]
    [InlineData(null, null, null, false, false)]
    [InlineData(" ", "accessKey", "http://topicurl.com", false, false)]
    [InlineData(null, "accessKey", "http://topicurl.com", false, false)]
    [InlineData("topicName", " ", "http://topicurl.com", false, false)]
    [InlineData("topicName", null, "http://topicurl.com", false, false)]
    [InlineData("topicName", "accessKey", " ", false, false)]
    [InlineData("topicName", "accessKey", null, false, false)]
    [InlineData("topicName", "accessKey", "topicEndpoint", false, false)]
    public void GivenEventPropagationConfigurations_WhenValidateOptions_ThenAccessCredentialsValidated(string topicName, string topicAccessKey, string topicEndpoint, bool useTokenCredential, bool optionsValid)
    {
        var validator = new EventPropagationPublisherOptionsValidator();

        var result = validator.Validate("namedOptions", new EventPropagationPublisherOptions
        {
            TokenCredential = useTokenCredential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicName = topicName,
            TopicAccessKey = topicAccessKey,
        });

        if (optionsValid)
        {
            Assert.True(result.Succeeded);
        }
        else
        {
            Assert.True(result.Failed);
        }
    }
}