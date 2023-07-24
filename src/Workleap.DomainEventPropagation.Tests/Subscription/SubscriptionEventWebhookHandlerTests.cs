using Azure.Messaging.EventGrid.SystemEvents;
using Moq;
using BindingFlags = System.Reflection.BindingFlags;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class SubscriptionEventWebhookHandlerTests
{
    private readonly Mock<ITelemetryClientProvider> _telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();

    [Fact]
    public void GivenEventGridSubscriptionEvent_WhenEventTopicIsNotSubscribedTo_ThenReturnRejectionResponse()
    {
        var subscriptionEventData = new Mock<SubscriptionValidationEventData>();

        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(false);

        var subscriptionEventWebhookHandler = new SubscriptionEventGridWebhookHandler(subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        var response = subscriptionEventWebhookHandler.HandleEventGridSubscriptionEvent(subscriptionEventData.Object, "eventType", "UnsubscribedTopic");

        Assert.Equal(default(SubscriptionValidationResponse), response);
    }

    [Fact]
    public void GivenEventGridSubscriptionEvent_WhenEventTopicIsSubscribedTo_ThenReturnAcceptResponse()
    {
        var validationCode = Guid.NewGuid();
        var subscriptionTopicValidatorMock = new Mock<ISubscriptionTopicValidator>();
        subscriptionTopicValidatorMock.Setup(x => x.IsSubscribedToTopic(It.IsAny<string>())).Returns(true);

        var type = typeof(SubscriptionValidationEventData);
        var subscriptionEvent = (SubscriptionValidationEventData)type.Assembly.CreateInstance(
            type.FullName, false,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { validationCode.ToString(), "url" }, null, null);

        var subscriptionEventWebhookHandler = new SubscriptionEventGridWebhookHandler(subscriptionTopicValidatorMock.Object, _telemetryClientProviderMock.Object);
        var response = subscriptionEventWebhookHandler.HandleEventGridSubscriptionEvent(subscriptionEvent, "eventType", "SubscribedTopic");

        Assert.NotNull(response);
        Assert.IsAssignableFrom<SubscriptionValidationResponse>(response);
        Assert.Equal(validationCode.ToString(), response.ValidationResponse);
    }
}