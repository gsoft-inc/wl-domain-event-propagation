namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class EventPropagationSubscriberOptionsTests
{
    private const string OrganizationTopicName = "Organization";
    private const string Signup = "Signup";

    [Fact]
    public void GivenEventPropagationSubscriberConfiguration_WhenAllTopicsAreValid_ThenPropertiesMatch()
    {
        var topics = new[] { Signup, OrganizationTopicName };
        var options = new EventPropagationSubscriberOptions { SubscribedTopics = topics };

        Assert.True(topics.SequenceEqual(options.SubscribedTopics));
    }
}