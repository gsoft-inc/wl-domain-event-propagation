using System.Threading.Channels;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using Workleap.DomainEventPropagation.Tests;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public sealed class PullDeliveryTests
{
    [Fact]
    public async Task OpenTelemetryActivityLinkIsPopulated()
    {
        var topicName = "Topic1";
        var subscriberName = "subscriber1";

        // Add a timeout for the test to not block indefinitely
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Enable context propagation and track activities
        EnableContextPropagation();
        using var activityTracker = new InMemoryActivityTracker();

        await using var emulator = await EmulatorContext.StartAsync(/*lang=json,strict*/ $$"""
               {
                 "Topics": {
                   "{{topicName}}": [
                     "pull://{{subscriberName}}"
                   ]
                 }
               }
               """);

        var serviceUrl = emulator.Url;
        
        // Configure the publisher and the subscriptions, and start the service
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Channel.CreateUnbounded<TestEvent>());
            services.AddEventPropagationPublisher(options =>
            {
                options.TopicEndpoint = serviceUrl;
                options.TopicName = topicName;
                options.TopicAccessKey = "noop";
                options.TopicType = TopicType.Namespace;
            });
            services.AddPullDeliverySubscription()
                .AddTopicSubscription("DummySectionName", options =>
                {
                    options.TopicEndpoint = serviceUrl;
                    options.TopicName = topicName;
                    options.SubscriptionName = subscriberName;
                    options.TopicAccessKey = "noop";
                })
                .AddDomainEventHandler<TestEvent, TestDomainEventHandler>();
        });

        var host = builder.Build();
        var runTask = host.RunAsync(cts.Token); // The method returns when the services are running

        // Send an event
        var client = host.Services.GetRequiredService<IEventPropagationClient>();
        await client.PublishDomainEventsAsync([new TestEvent { Id = 1 }], cts.Token);

        // Read the event. The background service should call TestDomainEventHandler, so the channel should have 1 item
        var channel = host.Services.GetRequiredService<Channel<TestEvent>>();
        var processedEvent = await channel.Reader.ReadAsync(cts.Token);
        Assert.Equal(1, processedEvent.Id);
        activityTracker.AssertSubscribeSuccessful("EventGridEvents process com.workleap.sample.testEvent");

        // terminate the service
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await runTask;
    }

    private static void EnableContextPropagation()
    {
        OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new BaggagePropagator(),
        }));
    }

    [DomainEvent("com.workleap.sample.testEvent", EventSchema.CloudEvent)]
    private sealed record TestEvent : IDomainEvent
    {
        public int Id { get; set; }
    }

    private sealed class TestDomainEventHandler(Channel<TestEvent> channel) : IDomainEventHandler<TestEvent>
    {
        public async Task HandleDomainEventAsync(TestEvent domainEvent, CancellationToken cancellationToken)
        {
            await channel.Writer.WriteAsync(domainEvent, cancellationToken);
        }
    }

    private sealed class EmulatorContext(IContainer container) : IAsyncDisposable
    {
        public string Url { get; } = $"http://localhost:{container.GetMappedPublicPort(6500)}/";

        public static async Task<EmulatorContext> StartAsync(string configuration)
        {
            var path = Path.GetTempFileName();
            await File.WriteAllTextAsync(path, configuration);

            var container = new ContainerBuilder()
                .WithImage("workleap/eventgridemulator")
                .WithPortBinding(6500, assignRandomHostPort: true)
                .WithBindMount(path, "/app/appsettings.json", AccessMode.ReadOnly)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6500))
                .Build();
            await container.StartAsync();
            
            return new EmulatorContext(container);
        }

        public async ValueTask DisposeAsync()
        {
            await container.DisposeAsync();
        }
    }
}