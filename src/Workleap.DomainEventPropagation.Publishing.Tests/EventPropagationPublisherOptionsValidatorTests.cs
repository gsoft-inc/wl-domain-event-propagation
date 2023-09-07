using Azure.Identity;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class EventPropagationPublisherOptionsValidatorTests
{
    [Theory]
    [InlineData(" ", "http://topicurl.com", true, true)]
    [InlineData(" ", "http://topicurl.com", false, false)]
    [InlineData("accessKey", "http://topicurl.com", true, true)]
    [InlineData("accessKey", "http://topicurl.com", false, true)]
    [InlineData(null, null, false, false)]
    [InlineData(null, "http://topicurl.com", false, false)]
    [InlineData("accessKey", " ", false, false)]
    [InlineData("accessKey", null, false, false)]
    [InlineData("accessKey", "topicEndpoint", false, false)]
    public void GivenEventPropagationConfigurations_WhenValidateOptions_ThenAccessCredentialsValidated(string topicAccessKey, string topicEndpoint, bool useTokenCredential, bool optionsValid)
    {
        var validator = new EventPropagationPublisherOptionsValidator();

        var result = validator.Validate("namedOptions", new EventPropagationPublisherOptions
        {
            TokenCredential = useTokenCredential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicAccessKey = topicAccessKey,
        });

        Assert.True(optionsValid ? result.Succeeded : result.Failed);
    }
}