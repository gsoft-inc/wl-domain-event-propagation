using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class EventStorePublisherTests()
{
    [Theory()]
    [InlineData(10000)]
    public async Task TestEventStore(int eventCount)
    {
        // Add a timeout for the test to not block indefinitely
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Configure the publisher and the subscriptions, and start the service
        var host = this.BuildHost();
        var runTask = host.RunAsync(cts.Token); // The method returns when the services are running

        // Send many events
        var client = host.Services.GetRequiredService<IResilientEventPropagationClient>();
        var eventBatches = Enumerable.Range(0, eventCount)
            .Chunk(1000)
            .Select(x => x.Select(id => new TestEvent { Id = id }).ToList());

        foreach (var eventBatch in eventBatches)
        {
            await client.PublishDomainEventsAsync(eventBatch, cts.Token);
        }

        var eventStore = host.Services.GetRequiredService<IEventStore>();
        var savedEvents = await eventStore.ReadEvents(cts.Token).ToListAsync();
        Assert.Equal(eventCount, savedEvents.Count);

        // Terminate the service
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await runTask;
    }

    private IHost BuildHost()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddEventPropagationPublisher(options =>
            {
                options.TopicEndpoint = "https://somekindofurl";
                options.TopicName = "Topic1";
                options.TopicAccessKey = "noop";
                options.TopicType = TopicType.Namespace;
            }).UseResilientEventPropagationPublisher<InMemoryEventStore>();
        });

        return builder.Build();
    }

    [DomainEvent("com.workleap.sample.slowTestEvent", EventSchema.CloudEvent)]
    private sealed record TestEvent : IDomainEvent
    {
        public int Id { get; set; }
    }
}