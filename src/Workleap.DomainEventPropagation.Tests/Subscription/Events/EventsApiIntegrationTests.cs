using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Events;

public class EventsApiIntegrationTests : IClassFixture<EventsApiIntegrationTestsFixture>
{
    private readonly HttpClient _httpClient;
    private readonly ITopicProvider _topicProvider;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public EventsApiIntegrationTests(EventsApiIntegrationTestsFixture fixture)
    {
        this._httpClient = fixture.CreateClient();
        this._topicProvider = fixture.TopicProvider;
    }

    /// <remarks>
    /// Unfortunately, <see cref="SubscriptionValidationEventData"/> only has internal
    /// constructors, which prevents us to use the real object. For tests purpose, it
    /// is understandable to just a declare a similar type considering their serialized
    /// representation will be identical.
    /// </remarks>
    public class SubscriptionValidationEventTestData
    {
        public string ValidationCode { get; init; }
    }

    [Fact]
    public async Task GivenEventsApi_WhenASubscriptionEventIsPosted_ThenReturnsOkWithValidationResponse()
    {
        A.CallTo(() => this._topicProvider.GetTopicValidationPattern(A<string>._)).Returns("dummytopic");

        var subscriptionValidationEventData = new SubscriptionValidationEventTestData
        {
            ValidationCode = "ABC"
        };

        var serializedContent = JsonSerializer.Serialize(
            new EventGridEvent(
                subject: "Blabla",
                eventType: SystemEventNames.EventGridSubscriptionValidation,
                dataVersion: "1.0",
                data: new BinaryData(subscriptionValidationEventData, SerializerOptions))
                {
                  Topic = EventsApiIntegrationTestsFixture.TestTopic,  
                },
            SerializerOptions);

        var content = new StringContent(serializedContent, Encoding.UTF8, "application/json");

        var response = await this._httpClient.PostAsync("/eventgrid/domainevents", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var subscriptionValidationResponse = await response.Content.ReadFromJsonAsync<SubscriptionValidationResponse>(SerializerOptions);

        Assert.Equal(subscriptionValidationEventData.ValidationCode, subscriptionValidationResponse.ValidationResponse);
    }

    [Fact]
    public async Task GivenEventsApi_WhenADomainEventIsPosted_ThenReturnsOk()
    {
        A.CallTo(() => this._topicProvider.GetTopicValidationPattern(A<string>._)).Returns("dummytopic");

        var dummyDomainEvent = new DummyDomainEvent
        {
            PropertyA = "A",
            PropertyB = 1
        };

        var eventGridEvent = new EventGridEvent(
            subject: typeof(DummyDomainEvent).FullName,
            eventType: typeof(DummyDomainEvent).FullName,
            dataVersion: "1.0",
            data: new BinaryData(dummyDomainEvent, SerializerOptions))
        {
            Topic = EventsApiIntegrationTestsFixture.TestTopic,
        };

        var content = new StringContent(JsonSerializer.Serialize(eventGridEvent), Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await this._httpClient.PostAsync("/eventgrid/domainevents", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class EventsApiIntegrationTestsFixture : WebApplicationFactory<EventsApiIntegrationTestsFixture.Startup>
{
    public const string TestTopic = "DummyTopic";

    public ITopicProvider TopicProvider = A.Fake<ITopicProvider>(p => p.Strict());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(TopicProvider);
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"{EventPropagationSubscriberOptions.SectionName}:SubscribedTopics:0"] = TestTopic,
            })
            .Build();

        builder.UseConfiguration(configuration);

        base.ConfigureWebHost(builder);
    }

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
                .AddDomainEventHandler<DummyDomainEventHandler>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.AddEventPropagationEndpoints();
            });
        }
    }
}