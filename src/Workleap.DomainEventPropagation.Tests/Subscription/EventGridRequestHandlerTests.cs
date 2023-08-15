using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using FakeItEasy;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class EventGridRequestHandlerTests
{
    [Fact]
    public async Task GivenEventGridRequest_WhenRequestContentNull_ThenThrowsException()
    {
        // Given
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridRequestHandler.HandleRequestAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValid_ThenAcceptResponseIsGenerated()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        A.CallTo(() => subscriptionEventGridWebhookHandler.HandleEventGridSubscriptionEvent(A<SubscriptionValidationEventData>._, A<string>._, A<string>._))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var result = await eventGridRequestHandler.HandleRequestAsync(GetEventGridSubscriptionRequest(validationCode), CancellationToken.None);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Response);
        Assert.Equal(validationCode, result.Response.ValidationResponse);
        Assert.Equal(EventGridRequestType.Subscription, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenExceptionIsThrown_ThenExceptionIsTracked()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        A.CallTo(() => subscriptionEventGridWebhookHandler.HandleEventGridSubscriptionEvent(A<SubscriptionValidationEventData>._, A<string>._, A<string>._))
            .Throws(new Exception("An exception was thrown"));

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var exception = await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(GetEventGridSubscriptionRequest(validationCode), CancellationToken.None));

        // Then
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValidAndContainsTelemetryCorrelationId_ThenRequestTelemetryParentIdIsNotSet()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        A.CallTo(() => subscriptionEventGridWebhookHandler.HandleEventGridSubscriptionEvent(A<SubscriptionValidationEventData>._, A<string>._, A<string>._))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var request = GetEventGridSubscriptionRequest(validationCode);
        var result = await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Response);
        Assert.Equal(validationCode, result.Response.ValidationResponse);
        Assert.Equal(EventGridRequestType.Subscription, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestContentValidAndContainsTelemetryCorrelationId_ThenRequestTelemetryParentIdIsSet()
    {
        // Given
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        A.CallTo(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(A<EventGridEvent>._, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var request = GetEventGridDomainEventRequest();
        var result = await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None);

        // Then
        Assert.NotNull(result);
        Assert.Equal(EventGridRequestType.Event, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestThrowsExceptionAndContainsTelemetryCorrelationId_ThenRequestTelemetryIsTracked()
    {
        // Given
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();
        A.CallTo(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(A<EventGridEvent>._, CancellationToken.None))
            .Throws(new Exception("An exception was thrown"));

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var request = GetEventGridDomainEventRequest();

        // Then
        await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestTelemetryNull_ThenOperationSucceeds()
    {
        // Given
        var domainEventGridWebhookHandler = A.Fake<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandler = A.Fake<ISubscriptionEventGridWebhookHandler>();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandler,
            subscriptionEventGridWebhookHandler);

        var request = GetEventGridDomainEventRequest();

        await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None);

        // Then
        A.CallTo(() => domainEventGridWebhookHandler.HandleEventGridWebhookEventAsync(A<EventGridEvent>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    private static string GetEventGridSubscriptionRequest(string validationCode)
    {
        var subscriptionRequest = @"[{
                'topic': '/subscriptions/topic-egt-',
                'subject': '',
                'eventType': 'Microsoft.EventGrid.SubscriptionValidationEvent',
                'eventTime': '2017-08-16T01:57:26.005121Z',
                'id': '602a88ef-0001-00e6-1233-1646070610ea',
                'data': {eventData},
                'metadataVersion': '1',
                'dataVersion': '1'
            }]";

        var eventData = @"{
                  'validationCode': '{validationCode}',
                  'validationUrl': 'https://validationurl.io'
            }";

        eventData = eventData.Replace("'", "\"");

        return subscriptionRequest.Replace("{eventData}", eventData).Replace("'", "\"").Replace("{validationCode}", validationCode);
    }

    private static string GetEventGridDomainEventRequest()
    {
        var domainEventRequest = @"[{
                'topic': '/subscriptions/topic-egt-',
                'subject': 'Test',
                'eventType': 'Workleap.Organization.DomainEvents.ExampleDomainEvent',
                'eventTime': '2017-08-16T01:57:26.005121Z',
                'id': '602a88ef-0001-00e6-1233-1646070610ea',
                'data': {eventData},
                'metadataVersion': '1',
                'dataVersion': '1'
             }]";

        var eventData = @"{
                  'eventDate': '2017-08-16T01:57:26.005121Z',
                  'eventId': 'b59ff87c-9bfb-46b7-9092-04735202d2f6'
                }";

        eventData = eventData.Replace("'", "\"");

        return domainEventRequest.Replace("{eventData}", eventData).Replace("'", "\"");
    }
}