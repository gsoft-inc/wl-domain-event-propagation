#nullable disable // To reproduce users that don't have nullable enabled

using Azure.Identity;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class EventPropagationSubscriptionOptionsValidatorTests
{
    [Theory]

    // Valid options
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", "subName", null, true)]

    // Access key
    [InlineData(" ", false, "http://topicurl.com", "topicName", "subName", null, false)]
    [InlineData(null, false, "http://topicurl.com", "topicName", "subName", null, false)]

    // Token credential
    [InlineData("accessKey", true, "http://topicurl.com", "topicName", "subName", null, true)]

    // Topic endpoint
    [InlineData("accessKey", false, "invalid-url", "topicName", "subName", null, false)]
    [InlineData("accessKey", false, null, "topicName", "subName", null, false)]
    [InlineData("accessKey", false, " ", "topicName", "subName", null, false)]

    // Topic name
    [InlineData("accessKey", false, "http://topicurl.com", " ", "subName", null, false)]
    [InlineData("accessKey", false, "http://topicurl.com", null, "subName", null, false)]

    // Subscription name
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", "", null, false)]
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", null, null, false)]

    // Max retries count
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", "", -1, false)]
    [InlineData("accessKey", false, "http://topicurl.com", "topicName", null, 11, false)]
    public void GivenNamedConfiguration_WhenValidate_ThenOptionsAreValidated(string topicAccessKey, bool useTokenCredential, string topicEndpoint, string topicName, string subName, int? maxRetriesCount, bool validationSucceeded)
    {
        var option = new EventPropagationSubscriptionOptions
        {
            TokenCredential = useTokenCredential ? new DefaultAzureCredential() : default,
            TopicEndpoint = topicEndpoint,
            TopicAccessKey = topicAccessKey,
            TopicName = topicName,
            SubscriptionName = subName
        };

        if (maxRetriesCount.HasValue)
        {
            option.MaxRetries = maxRetriesCount.Value;
        }

        var validator = new EventPropagationSubscriptionOptionsValidator();
        var result = validator.Validate(name: "namedOptions", options: option);

        Assert.Equal(expected: validationSucceeded, actual: result.Succeeded);
    }
}