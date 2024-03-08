using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using Workleap.DomainEventPropagation.Tests;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class SampleTests
{
    // TODO add timeout
    [Fact]
    public async Task Test()
    {
        OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new BaggagePropagator(),
        }));
        
        using var activityTracker = new InMemoryActivityTracker();
        // TODO uncomment
        // var path = Path.GetTempFileName();
        // await File.WriteAllTextAsync(path, /*lang=json,strict*/ """
        //     {
        //       "Topics": {
        //         "topic1": [
        //           "pull://subscriber1"
        //         ]
        //       }
        //     }
        //     """);

        // await using var container = new ContainerBuilder()
        //        .WithImage("workleap/eventgridemulator")
        //        .WithPortBinding(6500, assignRandomHostPort: true)
        //        .WithBindMount(path, "/app/appsettings.json", AccessMode.ReadOnly)
        //        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6500))
        //        .Build();
        // await container.StartAsync();

        //var url = $"http://localhost:{container.GetMappedPublicPort(6500)}/";
        var url = "http://localhost:6500/";

        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Channel.CreateUnbounded<TestEvent>());
            services.AddEventPropagationPublisher(options =>
            {
                options.TopicEndpoint = url;
                options.TopicName = "Topic1";
                options.TopicAccessKey = "dummy";
                options.TopicType = TopicType.Namespace;
            });
            services.AddPullDeliverySubscription()
                .AddTopicSubscription("DummySectionName", options =>
                {
                    options.TopicEndpoint = url;
                    options.TopicName = "Topic1";
                    options.SubscriptionName = "subscriber1";
                    options.TopicAccessKey = "dummy";
                })
                .AddDomainEventHandler<TestEvent, TestDomainEventHandler>();
        });

        var host = builder.Build();
        var runTask = host.RunAsync();

        var client = host.Services.GetRequiredService<IEventPropagationClient>();
        await client.PublishDomainEventsAsync([new TestEvent { Id = 1 }], CancellationToken.None);

        var channel = host.Services.GetRequiredService<Channel<TestEvent>>();
        var processedEvent = await channel.Reader.ReadAsync();
        Assert.Equal(1, processedEvent.Id);
        activityTracker.AssertSubscribeSuccessful("EventGridEvents process com.workleap.sample.testEvent");
        
        // terminate the service
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await runTask;
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
}
