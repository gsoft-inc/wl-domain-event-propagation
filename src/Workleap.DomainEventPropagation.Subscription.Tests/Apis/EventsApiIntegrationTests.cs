using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Workleap.DomainEventPropagation.Subscription.Tests.Apis;

[Collection(XunitCollectionConstants.StaticActivitySensitive)]
public class EventsApiIntegrationTests : IClassFixture<EventsApiIntegrationTestsFixture>
{
    private readonly HttpClient _httpClient;

    public EventsApiIntegrationTests(EventsApiIntegrationTestsFixture fixture)
    {
        this._httpClient = fixture.CreateClient();
    }

    [Fact]
    public async Task GivenEventsApi_WhenASubscriptionEventGridEventIsPosted_ThenReturnsOkWithValidationResponse()
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Given
        var subscriptionValidationEventData = new SubscriptionValidationEventTestData
        {
            ValidationCode = "ABC",
        };

        var eventGridEvent = new EventGridEvent(
            subject: "Blabla",
            eventType: SystemEventNames.EventGridSubscriptionValidation,
            dataVersion: "1.0",
            data: new BinaryData(subscriptionValidationEventData, serializerOptions))
        {
            Topic = EventsApiIntegrationTestsFixture.TestTopic,
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { eventGridEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var subscriptionValidationResponse = await response.Content.ReadFromJsonAsync<SubscriptionValidationResponse>(serializerOptions);

        Assert.NotNull(subscriptionValidationResponse);
        Assert.Equal(subscriptionValidationEventData.ValidationCode, subscriptionValidationResponse.ValidationResponse);
    }

    [Fact]
    public async Task GivenEventsApi_WhenASubscriptionCloudEventIsPosted_ThenReturnsOkWithValidationResponse()
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Given
        var subscriptionValidationEventData = new SubscriptionValidationEventTestData
        {
            ValidationCode = "ABC",
        };

        var cloudEvent = new CloudEvent(
            source: EventsApiIntegrationTestsFixture.TestTopic,
            type: SystemEventNames.EventGridSubscriptionValidation,
            data: new BinaryData(subscriptionValidationEventData, serializerOptions),
            dataContentType: "application/json")
        {
            Subject = "Blabla",
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { cloudEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var subscriptionValidationResponse = await response.Content.ReadFromJsonAsync<SubscriptionValidationResponse>(serializerOptions);

        Assert.NotNull(subscriptionValidationResponse);
        Assert.Equal(subscriptionValidationEventData.ValidationCode, subscriptionValidationResponse.ValidationResponse);
    }

    [Fact]
    public async Task GivenEventsApi_WhenADomainEventGridEventIsPosted_ThenReturnsOk()
    {
        // Given
        var wrapperEvent = DomainEventWrapper.Wrap(new DummyDomainEvent { PropertyB = 1, PropertyA = "Hello world" });

        var eventGridEvent = new EventGridEvent(
            subject: "subject",
            eventType: "event type",
            dataVersion: "1.0",
            data: new BinaryData(wrapperEvent.Data))
        {
            Topic = EventsApiIntegrationTestsFixture.TestTopic,
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { eventGridEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenEventsApi_WhenADomainCloudEventIsPosted_ThenReturnsOk()
    {
        // Given
        var wrapperEvent = DomainEventWrapper.Wrap(new DummyDomainEvent { PropertyB = 1, PropertyA = "Hello world" });

        var cloudEvent = new CloudEvent(
            source: EventsApiIntegrationTestsFixture.TestTopic,
            type: "event type",
            data: new BinaryData(wrapperEvent.Data),
            dataContentType: "application/json")
        {
            Subject = "blabla",
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { cloudEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenSecuredEventsApi_WhenADomainEventGridEventIsPostedWithoutAccessToken_ThenReturnsUnauthorized()
    {
        // Given
        var wrapperEvent = DomainEventWrapper.Wrap(new DummyDomainEvent { PropertyB = 1, PropertyA = "Hello world" });

        var eventGridEvent = new EventGridEvent(
            subject: wrapperEvent.DomainEventName,
            eventType: wrapperEvent.DomainEventName,
            dataVersion: "1.0",
            data: new BinaryData(wrapperEvent.Data))
        {
            Topic = EventsApiIntegrationTestsFixture.TestTopic,
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { eventGridEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenSecuredEventsApi_WhenADomainCloudEventIsPostedWithoutAccessToken_ThenReturnsUnauthorized()
    {
        // Given
        var wrapperEvent = DomainEventWrapper.Wrap(new DummyDomainEvent { PropertyB = 1, PropertyA = "Hello world" });

        var cloudEvent = new CloudEvent(
            source: EventsApiIntegrationTestsFixture.TestTopic,
            type: wrapperEvent.DomainEventName,
            data: new BinaryData(wrapperEvent.Data),
            dataContentType: "application/json")
        {
            Subject = wrapperEvent.DomainEventName,
        };

        // When
        var response = await this._httpClient.PostAsJsonAsync("/eventgrid/domainevents", new[] { cloudEvent });

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <remarks>
    /// Unfortunately, <see cref="SubscriptionValidationEventData"/> only has internal
    /// constructors, which prevents us to use the real object. For tests purpose, it
    /// is understandable to just a declare a similar type considering their serialized
    /// representation will be identical.
    /// </remarks>
    public class SubscriptionValidationEventTestData
    {
        public string ValidationCode { get; init; } = string.Empty;
    }
}

public sealed class EventsApiIntegrationTestsFixture : WebApplicationFactory<EventsApiIntegrationTestsFixture.Startup>
{
    public const string TestTopic = "DummyTopic";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
        });

        var configuration = new ConfigurationBuilder().Build();

        builder.UseConfiguration(configuration);

        base.ConfigureWebHost(builder);
    }

    /// <remarks>
    /// Given we are in a test assembly and that we don't have a typical entry point project,
    /// we manually create a host like it would be done in consuming applications, which would
    /// register the dependencies pertaining to the subscriber capabilities.
    /// </summary>
    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddEventPropagationSubscriber()
                .AddDomainEventHandler<DummyDomainEvent, DummyDomainEventHandler>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapEventPropagationEndpoint();
            });
        }
    }
}