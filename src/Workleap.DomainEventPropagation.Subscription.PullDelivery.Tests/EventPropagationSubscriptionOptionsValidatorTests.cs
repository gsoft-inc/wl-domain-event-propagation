#nullable disable // To reproduce users that don't have nullable enabled

using Azure.Identity;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class EventPropagationSubscriptionOptionsValidatorTests
{
    [Theory]

    // Valid options
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", "subName", true)]

    // Access key
    [InlineData(" ", false, "http://topicurl.com", "topicName", "subName", false)]
    [InlineData(null, false, "http://topicurl.com", "topicName", "subName", false)]

    // Token credential
    [InlineData("accessKey", true, "http://topicurl.com", "topicName", "subName", true)]

    // Topic endpoint
    [InlineData("accessKey", false, "invalid-url", "topicName", "subName", false)]
    [InlineData("accessKey", false, null, "topicName", "subName", false)]
    [InlineData("accessKey", false, " ", "topicName", "subName", false)]

    // Topic name
    [InlineData("accessKey", false, "http://topicurl.com", " ", "subName", false)]
    [InlineData("accessKey", false, "http://topicurl.com", null, "subName", false)]

    // Subscription name
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", "", false)]
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", null, false)]
    public void GivenNamedConfiguration_WhenValidate_ThenOptionsAreValidated(string topicAccessKey, bool useTokenCredential, string topicEndpoint, string topicName, string subName, bool validationSucceeded)
    {
        var validator = new EventPropagationSubscriptionOptionsValidator();

        var result = validator.Validate("namedOptions", new EventPropagationSubscriptionOptions
        {
            TokenCredential = useTokenCredential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicAccessKey = topicAccessKey,
            TopicName = topicName,
            SubscriptionName = subName,
        });

        Assert.Equal(validationSucceeded, result.Succeeded);
    }
}