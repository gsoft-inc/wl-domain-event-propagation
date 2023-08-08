using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Moq;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class EventGridRequestHandlerTests
{
    [Fact]
    public async Task GivenEventGridRequest_WhenRequestContentNull_ThenThrowsException()
    {
        // Given
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();

        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridRequestHandler.HandleRequestAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValid_ThenAcceptResponseIsGenerated()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

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
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Cool exception"));

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

        var exception = await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(GetEventGridSubscriptionRequest(validationCode), CancellationToken.None));

        // Then
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValidAndContainsTelemetryCorrelationId_ThenRequestTelemetryParentIdIsNotSet()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

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
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        domainEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None)).Returns(Task.CompletedTask);

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

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
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        domainEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None)).Throws(new Exception("Never in a million years will I let tobbacco touch my lips"));

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

        var request = GetEventGridDomainEventRequest();

        // Then
        await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestTelemetryNull_ThenOperationSucceeds()
    {
        // Given
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object);

        var request = GetEventGridDomainEventRequest();

        await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None);

        // Then
        domainEventGridWebhookHandlerMock.Verify(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None), Times.Once);
    }

    private static string GetEventGridSubscriptionRequest(string validationCode)
    {
        var subscriptionRequest = @"[{
                'topic': '/subscriptions/Organization-egt-',
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
                  'validationUrl': 'https://rp-eastus2.eventgrid.azure.net:553/eventsubscriptions/estest/validate?id=512d38b6-c7b8-40c8-89fe-f46f9e9622b6&t=2018-04-26T20:30:54.4538837Z&apiVersion=2018-05-01-preview&token=1A1A1A1A'
            }";

        eventData = eventData.Replace("'", "\"");

        return subscriptionRequest.Replace("{eventData}", eventData).Replace("'", "\"").Replace("{validationCode}", validationCode);
    }

    private static string GetEventGridDomainEventRequest()
    {
        var domainEventRequest = @"[{
                'topic': '/subscriptions/Organization-egt-',
                'subject': 'Test',
                'eventType': 'Workleap.Organization.DomainEvents.ExampleDomainEvent',
                'eventTime': '2017-08-16T01:57:26.005121Z',
                'id': '602a88ef-0001-00e6-1233-1646070610ea',
                'data': {eventData},
                'metadataVersion': '1',
                'dataVersion': '1'
             }]";

        var eventData = @"{
                  'coolDate': '2017-08-16T01:57:26.005121Z',
                  'coolId': 'b59ff87c-9bfb-46b7-9092-04735202d2f6'
                }";

        eventData = eventData.Replace("'", "\"");

        return domainEventRequest.Replace("{eventData}", eventData).Replace("'", "\"");
    }
}