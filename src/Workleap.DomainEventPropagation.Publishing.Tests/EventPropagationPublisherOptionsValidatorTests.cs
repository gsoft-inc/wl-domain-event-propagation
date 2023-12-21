#nullable disable // To reproduce users that don't have nullable enabled

using Azure.Identity;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class EventPropagationPublisherOptionsValidatorTests
{
    [Theory]
    [InlineData(TopicType.Default, " ", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Default, " ", "http://topicurl.com", "topicName", false, false)]
    [InlineData(TopicType.Default, "accessKey", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Default, "accessKey", "http://topicurl.com", "topicName", false, true)]
    [InlineData(TopicType.Default, null, null, "topicName", false, false)]
    [InlineData(TopicType.Default, null, "http://topicurl.com", "topicName", false, false)]
    [InlineData(TopicType.Default, "accessKey", " ", "topicName", false, false)]
    [InlineData(TopicType.Default, "accessKey", null, "topicName", false, false)]
    [InlineData(TopicType.Default, "accessKey", "topicEndpoint", "topicName", false, false)]
    [InlineData(TopicType.Default, "accessKey", "http://topicurl.com", null, true, true)]
    [InlineData(TopicType.Default, "accessKey", "http://topicurl.com", " ", true, true)]
    [InlineData(TopicType.Namespace, "accessKey", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Namespace, "accessKey", "http://topicurl.com", "", true, false)]
    public void GivenEventPropagationConfigurations_WhenValidateOptions_ThenAccessCredentialsValidated(TopicType topicType, string topicAccessKey, string topicEndpoint, string topicName, bool useTokenCredential, bool optionsValid)
    {
        var validator = new EventPropagationPublisherOptionsValidator();

        var result = validator.Validate("namedOptions", new EventPropagationPublisherOptions
        {
            TopicType = topicType,
            TokenCredential = useTokenCredential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicAccessKey = topicAccessKey,
            TopicName = topicName,
        });

        Assert.True(optionsValid ? result.Succeeded : result.Failed);
    }
}