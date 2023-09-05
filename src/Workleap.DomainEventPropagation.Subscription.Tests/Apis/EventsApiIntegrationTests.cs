using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
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

public class EventsApiIntegrationTests : IClassFixture<EventsApiIntegrationTestsFixture>
{
    private readonly HttpClient _httpClient;

    public EventsApiIntegrationTests(EventsApiIntegrationTestsFixture fixture)
    {
        this._httpClient = fixture.CreateClient();
    }

    [Fact]
    public async Task GivenEventsApi_WhenASubscriptionEventIsPosted_ThenReturnsOkWithValidationResponse()
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
    public async Task GivenEventsApi_WhenADomainEventIsPosted_ThenReturnsOk()
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
    public async Task GivenSecuredEventsApi_WhenADomainEventIsPostedWithoutAccessToken_ThenReturnsUnauthorized()
    {
        // Given
        var wrapperEvent = DomainEventWrapper.Wrap(new DummyDomainEvent { PropertyB = 1, PropertyA = "Hello world" });

        var eventGridEvent = new EventGridEvent(
            subject: typeof(DummyDomainEvent).FullName,
            eventType: wrapperEvent.GetType().FullName,
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