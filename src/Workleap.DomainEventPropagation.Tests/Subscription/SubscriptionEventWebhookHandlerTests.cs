using Azure.Messaging.EventGrid.SystemEvents;
using Moq;
using BindingFlags = System.Reflection.BindingFlags;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class SubscriptionEventWebhookHandlerTests
{
    [Fact]
    public void GivenEventGridSubscriptionEvent_WhenHandleEventGridSubscriptionEvent_ThenReturnAcceptResponse()
    {
        var validationCode = Guid.NewGuid();

        var type = typeof(SubscriptionValidationEventData);
        var subscriptionEvent = (SubscriptionValidationEventData)type.Assembly.CreateInstance(
            type.FullName!,
            ignoreCase: false,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new object[] { validationCode.ToString(), "url" },
            null,
            null)!;

        var subscriptionEventWebhookHandler = new SubscriptionEventGridWebhookHandler();
        var response = subscriptionEventWebhookHandler.HandleEventGridSubscriptionEvent(subscriptionEvent, "eventType", "SubscribedTopic");

        Assert.NotNull(response);
        Assert.IsAssignableFrom<SubscriptionValidationResponse>(response);
        Assert.Equal(validationCode.ToString(), response.ValidationResponse);
    }
}