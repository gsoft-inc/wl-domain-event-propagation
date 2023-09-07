using System.Net;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using FakeItEasy;

namespace Workleap.DomainEventPropagation.Subscription.Tests.Apis;

public class EventsApiUnitTests
{
    private readonly IEventGridRequestHandler _eventGridRequestHandler;

    public EventsApiUnitTests()
    {
        this._eventGridRequestHandler = A.Fake<IEventGridRequestHandler>(x => x.Strict());
    }

    [Fact]
    public async Task GivenEventsApiHandleEvent_WhenResultRequestTypeIsSubscription_ThenReturnsOkResultWithValidationCode()
    {
        // Given
        var subscriptionValidationResponse = new SubscriptionValidationResponse
        {
            ValidationResponse = "abcdef",
        };

        var eventGridRequestResult = new EventGridRequestResult(EventGridRequestType.Subscription, subscriptionValidationResponse);

        A.CallTo(() => this._eventGridRequestHandler.HandleRequestAsync(A<EventGridEvent[]>._, A<CancellationToken>._)).Returns(Task.FromResult(eventGridRequestResult));

        // When
        var actualResult = await EventsApi.HandleEventGridEvents(Array.Empty<EventGridEvent>(), this._eventGridRequestHandler, CancellationToken.None);

        // Then
        var (data, statusCode) = await actualResult.GetResponseAsync<SubscriptionValidationResponse>();

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(data);
        Assert.Equal(subscriptionValidationResponse.ValidationResponse, data.ValidationResponse);
    }

    [Fact]
    public async Task GivenEventsApiHandleEvent_WhenResultRequestTypeIsNotSubscription_ThenReturnsOkResult()
    {
        // Given
        var eventGridRequestResult = new EventGridRequestResult(EventGridRequestType.Event);

        A.CallTo(() => this._eventGridRequestHandler.HandleRequestAsync(A<EventGridEvent[]>._, A<CancellationToken>._)).Returns(Task.FromResult(eventGridRequestResult));

        // When
        var actualResult = await EventsApi.HandleEventGridEvents(Array.Empty<EventGridEvent>(), this._eventGridRequestHandler, CancellationToken.None);

        // Then
        var (data, statusCode) = await actualResult.GetResponseAsync<object>();

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.Null(data);
    }
}