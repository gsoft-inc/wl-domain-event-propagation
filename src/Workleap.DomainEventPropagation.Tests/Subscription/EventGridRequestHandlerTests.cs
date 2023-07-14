using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.ApplicationInsights.DataContracts;
using Moq;
using OpenTelemetry.Trace;
using Workleap.DomainEventPropagation.AzureSystemEvents;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class EventGridRequestHandlerTests
{
    [Fact]
    public async Task GivenEventGridRequest_WhenRequestContentNull_ThenThrowsException()
    {
        // Given
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();

        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => eventGridRequestHandler.HandleRequestAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValid_ThenAcceptResponseIsGenerated()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var result = await eventGridRequestHandler.HandleRequestAsync(GetEventGridSubscriptionRequest(validationCode), CancellationToken.None);

        // Then
        Assert.NotNull(result);
        Assert.Equal(validationCode, result.Response.ValidationResponse);
        Assert.Equal(EventGridRequestType.Subscription, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenExceptionIsThrown_ThenExceptionIsTracked()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Cool exception"));

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var exception = await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(GetEventGridSubscriptionRequest(validationCode), CancellationToken.None));

        // Then
        Assert.NotNull(exception);
        telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }

    [Fact]
    public async Task GivenSubscriptionEventGridRequest_WhenRequestContentValidAndContainsTelemetryCorrelationId_ThenRequestTelemetryParentIdIsNotSet()
    {
        // Given
        var validationCode = Guid.NewGuid().ToString();
        var telemetryCorrelationId = Guid.NewGuid().ToString();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        subscriptionEventGridWebhookHandlerMock
            .Setup(x => x.HandleEventGridSubscriptionEvent(It.IsAny<SubscriptionValidationEventData>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new SubscriptionValidationResponse { ValidationResponse = validationCode });

        var requestTelemetry = new RequestTelemetry();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridSubscriptionRequest(validationCode, telemetryCorrelationId);
        var result = await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(requestTelemetry);
        Assert.Null(requestTelemetry.Context.Operation.ParentId);
        Assert.Equal(validationCode, result.Response.ValidationResponse);
        Assert.Equal(EventGridRequestType.Subscription, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestContentValidAndContainsTelemetryCorrelationId_ThenRequestTelemetryParentIdIsSet()
    {
        // Given
        var telemetryCorrelationId = Guid.NewGuid().ToString();

        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        domainEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None)).Returns(Task.CompletedTask);

        var requestTelemetry = new RequestTelemetry();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridDomainEventRequest(telemetryCorrelationId);
        var result = await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(requestTelemetry);
        Assert.Equal(requestTelemetry.Context.Operation.ParentId, telemetryCorrelationId);
        Assert.True(requestTelemetry.Success != null && requestTelemetry.Success.Value);
        Assert.Equal(EventGridRequestType.Event, result.EventGridRequestType);
    }

    [Fact]
    public async Task GivenAzureSystemEventEventGridRequest_WhenRequestContentValid_ThenOperationSucceeds()
    {
        // Given
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();

        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        azureSystemEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), It.IsAny<object>(), CancellationToken.None)).Returns(Task.CompletedTask);

        var requestTelemetry = new RequestTelemetry();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridAzureSystemEventRequest();
        var result = await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(requestTelemetry);
        Assert.Equal(null, requestTelemetry.Context.Operation.ParentId);
        Assert.True(requestTelemetry.Success != null && requestTelemetry.Success.Value);
        Assert.Equal(EventGridRequestType.Event, result.EventGridRequestType);

        azureSystemEventGridWebhookHandlerMock.Verify(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), It.IsAny<object>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestThrowsExceptionAndContainsTelemetryCorrelationId_ThenRequestTelemetryIsTracked()
    {
        // Given
        var telemetryCorrelationId = Guid.NewGuid().ToString();

        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();

        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        domainEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None)).Throws(new Exception("Never in a million years will I let tobbacco touch my lips"));

        var requestTelemetry = new RequestTelemetry();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridDomainEventRequest(telemetryCorrelationId);

        await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry));

        // Then
        Assert.NotNull(requestTelemetry);
        Assert.Equal(requestTelemetry.Context.Operation.ParentId, telemetryCorrelationId);
        Assert.False(requestTelemetry.Success != null && requestTelemetry.Success.Value);

        telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }

    [Fact]
    public async Task GivenAzureSystemEventEventGridRequest_WhenRequestThrowsException_ThenExceptionIsTracked()
    {
        // Given
        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();

        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();
        azureSystemEventGridWebhookHandlerMock.Setup(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), It.IsAny<object>(), CancellationToken.None)).Throws(new Exception("Never in a million years will I let tobbacco touch my lips"));

        var requestTelemetry = new RequestTelemetry();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridAzureSystemEventRequest();

        await Assert.ThrowsAsync<Exception>(() => eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry));

        // Then
        Assert.NotNull(requestTelemetry);
        Assert.Equal(null, requestTelemetry.Context.Operation.ParentId);
        Assert.False(requestTelemetry.Success != null && requestTelemetry.Success.Value);

        telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Once);
    }

    [Fact]
    public async Task GivenDomainEventEventGridRequest_WhenRequestTelemetryNull_ThenOperationSucceeds()
    {
        // Given
        var telemetryCorrelationId = Guid.NewGuid().ToString();

        var subscriptionEventGridWebhookHandlerMock = new Mock<ISubscriptionEventGridWebhookHandler>();
        var telemetryClientProviderMock = new Mock<ITelemetryClientProvider>();
        var domainEventGridWebhookHandlerMock = new Mock<IDomainEventGridWebhookHandler>();
        var azureSystemEventGridWebhookHandlerMock = new Mock<IAzureSystemEventGridWebhookHandler>();

        // When
        var eventGridRequestHandler = new EventGridRequestHandler(
            domainEventGridWebhookHandlerMock.Object,
            azureSystemEventGridWebhookHandlerMock.Object,
            subscriptionEventGridWebhookHandlerMock.Object,
            telemetryClientProviderMock.Object);

        var request = GetEventGridDomainEventRequest(telemetryCorrelationId);

        await eventGridRequestHandler.HandleRequestAsync(request, CancellationToken.None, requestTelemetry: null);

        // Then
        domainEventGridWebhookHandlerMock.Verify(x => x.HandleEventGridWebhookEventAsync(It.IsAny<EventGridEvent>(), CancellationToken.None), Times.Once);
        telemetryClientProviderMock.Verify(x => x.TrackException(It.IsAny<Exception>(), It.IsAny<TelemetrySpan>()), Times.Never);
    }

    private static string GetEventGridSubscriptionRequest(string validationCode, string telemetryCorrelationId = "")
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

        if (!string.IsNullOrEmpty(telemetryCorrelationId))
        {
            eventData = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(eventData, telemetryCorrelationId);
        }

        return subscriptionRequest.Replace("{eventData}", eventData).Replace("'", "\"").Replace("{validationCode}", validationCode);
    }

    private static string GetEventGridDomainEventRequest(string telemetryCorrelationId = "")
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

        if (!string.IsNullOrEmpty(telemetryCorrelationId))
        {
            eventData = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(eventData, telemetryCorrelationId);
        }

        return domainEventRequest.Replace("{eventData}", eventData).Replace("'", "\"");
    }

    private static string GetEventGridAzureSystemEventRequest()
    {
        var subscriptionRequest = @"[{
                'topic': '/subscriptions/1234/resourceGroups/ov-dev-something/providers/Microsoft.Media/mediaservices/xzxzxzx-egst-xzxzxz',
                'subject': 'transforms/VideoAnalyzerTransform/jobs/12345',
                'eventType': 'Microsoft.Media.JobFinished',
                'eventTime': '2022-08-14T01:57:26.005121Z',
                'id': '602a88ef-0001-00e6-1233-1646070610ea',
                'data': {eventData},
                'metadataVersion': '1',
                'dataVersion': '1'
            }]";

        var eventData = @"{ 'outputs': [] }";

        eventData = eventData.Replace("'", "\"");

        return subscriptionRequest.Replace("{eventData}", eventData).Replace("'", "\"");
    }
}