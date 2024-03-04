#nullable disable // To reproduce users that don't have nullable enabled

using Azure.Identity;

namespace Workleap.DomainEventPropagation.Publishing.Tests;

public class EventPropagationPublisherOptionsValidatorTests
{
    [Theory]
    [InlineData(TopicType.Custom, " ", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Custom, " ", "http://topicurl.com", "topicName", false, false)]
    [InlineData(TopicType.Custom, "accessKey", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Custom, "accessKey", "http://topicurl.com", "topicName", false, true)]
    [InlineData(TopicType.Custom, null, null, "topicName", false, false)]
    [InlineData(TopicType.Custom, null, "http://topicurl.com", "topicName", false, false)]
    [InlineData(TopicType.Custom, "accessKey", " ", "topicName", false, false)]
    [InlineData(TopicType.Custom, "accessKey", null, "topicName", false, false)]
    [InlineData(TopicType.Custom, "accessKey", "topicEndpoint", "topicName", false, false)]
    [InlineData(TopicType.Custom, "accessKey", "http://topicurl.com", null, true, true)]
    [InlineData(TopicType.Custom, "accessKey", "http://topicurl.com", " ", true, true)]
    [InlineData(TopicType.Namespace, "accessKey", "http://topicurl.com", "topicName", true, true)]
    [InlineData(TopicType.Namespace, "accessKey", "http://topicurl.com", "", true, false)]
    [InlineData(TopicType.Namespace, "accessKey", "http://topicurl.com", null, true, false)]
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