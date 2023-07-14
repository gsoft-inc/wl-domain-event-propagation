using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using Moq;
using Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class SubscriptionTopicValidatorTests
{
    private readonly Mock<ITopicProvider> _topicProviderMock = new();

    private sealed record Topic(string Name, string Pattern);

    private static readonly Topic OrganizationTopic = new("Organization", "my-organization-pattern");
    private static readonly Topic SignupTopic = new("Signup", "my-signup-pattern");
    private static readonly Topic IntegrationsTopic = new("Integrations", "my-integrations-pattern");

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenTopicNotInEventPropagationSubscriberOptions_TheReturnFalse()
    {
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(OrganizationTopic.Name)).Returns(OrganizationTopic.Pattern);
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(SignupTopic.Name)).Returns(SignupTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { OrganizationTopic.Name, SignupTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { _topicProviderMock.Object });

        Assert.False(subscriptionTopicValidator.IsSubscribedToTopic(IntegrationsTopic.Pattern));
    }

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenTopicInEventPropagationSubscriberOptions_TheReturnTrue()
    {
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(OrganizationTopic.Name)).Returns(OrganizationTopic.Pattern);
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(SignupTopic.Name)).Returns(SignupTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { OrganizationTopic.Name, SignupTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { _topicProviderMock.Object });

        Assert.True(subscriptionTopicValidator.IsSubscribedToTopic(OrganizationTopic.Pattern));
        Assert.True(subscriptionTopicValidator.IsSubscribedToTopic(SignupTopic.Pattern));
    }

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenTopicFromEventGridEventNotInEventPropagationSubscriberOptions_TheReturnFalse()
    {
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(OrganizationTopic.Name)).Returns(OrganizationTopic.Pattern);
        _topicProviderMock.Setup(x => x.GetTopicValidationPattern(SignupTopic.Name)).Returns(SignupTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { OrganizationTopic.Name, SignupTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { _topicProviderMock.Object });

        Assert.False(subscriptionTopicValidator.IsSubscribedToTopic(GetDummyEventGridEventWithTopic(IntegrationsTopic.Pattern)));
    }

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenTopicFromEventGridEventInEventPropagationSubscriberOptions_TheReturnTrue()
    {
        var topicProviderMock = new Mock<ITopicProvider>();
        topicProviderMock.Setup(x => x.GetTopicValidationPattern(OrganizationTopic.Name)).Returns(OrganizationTopic.Pattern);
        topicProviderMock.Setup(x => x.GetTopicValidationPattern(SignupTopic.Name)).Returns(SignupTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { OrganizationTopic.Name, SignupTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { topicProviderMock.Object });

        Assert.True(subscriptionTopicValidator.IsSubscribedToTopic(GetDummyEventGridEventWithTopic(OrganizationTopic.Pattern)));
        Assert.True(subscriptionTopicValidator.IsSubscribedToTopic(GetDummyEventGridEventWithTopic(SignupTopic.Pattern)));
    }

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenSubscribedToSignup_ThenAcceptSignupEventsOnly()
    {
        var topicProviderMock = new Mock<ITopicProvider>();
        topicProviderMock.Setup(x => x.GetTopicValidationPattern(SignupTopic.Name)).Returns(SignupTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { SignupTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { topicProviderMock.Object });

        Assert.True(subscriptionTopicValidator.IsSubscribedToTopic(GetDummyEventGridEventWithTopic("/subscriptions/9c9e4c70-e581-420a-8906-ef2a37e02d94/resourceGroups/wl-dev/providers/Microsoft.EventGrid/topics/wl-my-SiGnUp-pattern-npfxkgdpyqtg6")));
    }

    [Fact]
    public void GivenSubscriptionTopicValidator_WhenSubscribedToOrganization_ThenRefuseSignupEvents()
    {
        var topicProviderMock = new Mock<ITopicProvider>();
        topicProviderMock.Setup(x => x.GetTopicValidationPattern(OrganizationTopic.Name)).Returns(OrganizationTopic.Pattern);

        var options = new EventPropagationSubscriberOptions { SubscribedTopics = new[] { OrganizationTopic.Name } };
        var optionsWrapper = new OptionsWrapper<EventPropagationSubscriberOptions>(options);
        var subscriptionTopicValidator = new SubscriptionTopicValidator(optionsWrapper, new [] { topicProviderMock.Object});

        Assert.False(subscriptionTopicValidator.IsSubscribedToTopic(GetDummyEventGridEventWithTopic("/subscriptions/9c9e4c70-e581-420a-8906-ef2a37e02d94/resourceGroups/wl-dev/providers/Microsoft.EventGrid/topics/wl-my-signup-patterm-npfxkgdpyqtg6")));
    }

    private static EventGridEvent GetDummyEventGridEventWithTopic(string topic)
    {
        return new EventGridEvent("subject", "eventType", "dataVersion", new TestDomainEvent()) { Topic = topic };
    }
}