using System.Net;
using Azure.Messaging.EventGrid.SystemEvents;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Workleap.DomainEventPropagation.Events;
using Workleap.DomainEventPropagation.Tests.Helpers;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Events;

public class EventsApiUnitTests
{
    private readonly HttpContext _httpContext;
    private readonly IEventGridRequestHandler _eventGridRequestHandler;

    public EventsApiUnitTests()
    {
        this._httpContext = A.Fake<HttpContext>(x => x.Strict());
        this._eventGridRequestHandler = A.Fake<IEventGridRequestHandler>(x => x.Strict());
    }

    [Fact]
    public async Task GivenEventsApiHandleEvent_WhenResultRequestTypeIsSubscription_ThenReturnsOkResultWithValidationCode()
    {
        // Given
        var httpContextFeatures = A.Fake<IFeatureCollection>();

        A.CallTo(() => this._httpContext.Features).Returns(httpContextFeatures);

        var subscriptionValidationResponse = new SubscriptionValidationResponse
        {
            ValidationResponse = "abcdef",
        };

        var eventGridRequestResult = new EventGridRequestResult
        {
            EventGridRequestType = EventGridRequestType.Subscription,
            Response = subscriptionValidationResponse,
        };

        A.CallTo(() => this._eventGridRequestHandler.HandleRequestAsync(A<object>._, A<CancellationToken>._)).Returns(Task.FromResult(eventGridRequestResult));

        // When
        var actualResult = await EventsApi.HandleEventGridEvent(new object(), this._httpContext, this._eventGridRequestHandler, CancellationToken.None);

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
        var httpContextFeatures = A.Fake<IFeatureCollection>();

        A.CallTo(() => this._httpContext.Features).Returns(httpContextFeatures);

        var eventGridRequestResult = new EventGridRequestResult
        {
            EventGridRequestType = EventGridRequestType.Event,
        };

        A.CallTo(() => this._eventGridRequestHandler.HandleRequestAsync(A<object>._, A<CancellationToken>._)).Returns(Task.FromResult(eventGridRequestResult));

        // When
        var actualResult = await EventsApi.HandleEventGridEvent(new object(), this._httpContext, this._eventGridRequestHandler, CancellationToken.None);

        // Then
        var (data, statusCode) = await actualResult.GetResponseAsync<object>();

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.Null(data);
    }
}