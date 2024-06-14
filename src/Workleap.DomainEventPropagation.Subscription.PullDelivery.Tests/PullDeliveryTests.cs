using System.Diagnostics;
using System.Threading.Channels;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Workleap.DomainEventPropagation.Tests;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class PullDeliveryTests(ITestOutputHelper testOutputHelper)
{
    private const int EmulatorPort = 6500;
    private const string TopicName = "Topic1";
    private const string SubscriberName = "subscriber1";
    private const int EventId = 1;
    private const int SlowHandlerDelay = 250;
    
    [Fact]
    public async Task TestPublishAndReceiveEvent()
    {
        // Add a timeout for the test to not block indefinitely
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Enable activity tracking
        using var activityTracker = new InMemoryActivityTracker();
        
        // Start Event Grid Emulator
        await using var eventGridEmulator = await EventGridEmulatorContext.StartAsync(testOutputHelper);
        
        // Configure the publisher and the subscriptions, and start the service
        var host = this.BuildHost(eventGridEmulator.Url);
        var runTask = host.RunAsync(cts.Token); // The method returns when the services are running
        
        // Send an event
        var client = host.Services.GetRequiredService<IEventPropagationClient>();
        await client.PublishDomainEventsAsync([new TestEvent { Id = EventId }], cts.Token);
        activityTracker.AssertPublishSuccessful("CloudEvents create com.workleap.sample.testEvent");

        // Read the event. The background service should call TestDomainEventHandler, so the channel should have 1 item
        var channel = host.Services.GetRequiredService<Channel<TestEvent>>();
        var processedEvent = await channel.Reader.ReadAsync(cts.Token);
        Assert.Equal(EventId, processedEvent.Id);
        activityTracker.AssertSubscribeSuccessful("CloudEvents process com.workleap.sample.testEvent");
        
        // Terminate the service
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await runTask;
    }

    [Theory(Skip = "Too long for the CI, only used locally to test performance")]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task TestHeavyLoadOfEvents(int eventCount)
    {
        // Add a timeout for the test to not block indefinitely
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Start Event Grid Emulator
        await using var eventGridEmulator = await EventGridEmulatorContext.StartAsync(testOutputHelper);

        // Configure the publisher and the subscriptions, and start the service
        var host = this.BuildHost(eventGridEmulator.Url);
        var runTask = host.RunAsync(cts.Token); // The method returns when the services are running

        // Get the event processing output. The background service should call SlowTestDomainEventHandler, so the data should get updated
        var eventProcessingOutput = host.Services.GetRequiredService<EventProcessingOutput>();

        // Send many events
        var client = host.Services.GetRequiredService<IEventPropagationClient>();
        var eventBatches = Enumerable.Range(0, eventCount)
            .Chunk(1000)
            .Select(x => x.Select(id => new SlowTestEvent { Id = id }).ToList());

        foreach (var eventBatch in eventBatches)
        {
            eventProcessingOutput.IncrementTotal(eventBatch.Count);
            await client.PublishDomainEventsAsync(eventBatch, cts.Token);
        }

        // Wait for all events to be processed
        await eventProcessingOutput.WaitForCompletion(cts.Token);
        testOutputHelper.WriteLine($"Processed {eventProcessingOutput.Count} out of {eventProcessingOutput.Total} events in {eventProcessingOutput.Duration.TotalMilliseconds} ms ({eventProcessingOutput.EventsPerSecond:F} event/second) with handler execution time at {SlowHandlerDelay} ms");
        Assert.True(eventProcessingOutput.EventsPerSecond > 1m / SlowHandlerDelay);

        // Terminate the service
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await runTask;
    }

    private IHost BuildHost(string eventGridUrl)
    {
        IHostBuilder? builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Channel.CreateUnbounded<TestEvent>());
            services.AddSingleton<EventProcessingOutput>();
            services.AddEventPropagationPublisher(options =>
            {
                options.TopicEndpoint = eventGridUrl;
                options.TopicName = TopicName;
                options.TopicAccessKey = "noop";
                options.TopicType = TopicType.Namespace;
            });
            services.AddPullDeliverySubscription()
                .AddTopicSubscription("DummySectionName", options =>
                {
                    options.TopicEndpoint = eventGridUrl;
                    options.TopicName = TopicName;
                    options.SubscriptionName = SubscriberName;
                    options.TopicAccessKey = "noop";
                    options.MaxDegreeOfParallelism = 500;
                })
                .AddDomainEventHandler<TestEvent, TestDomainEventHandler>()
                .AddDomainEventHandler<SlowTestEvent, SlowTestDomainEventHandler>();
        });

        return builder.Build();
    }

    [DomainEvent("com.workleap.sample.testEvent", EventSchema.CloudEvent)]
    private sealed record TestEvent : IDomainEvent
    {
        public int Id { get; set; }
    }

    [DomainEvent("com.workleap.sample.slowTestEvent", EventSchema.CloudEvent)]
    private sealed record SlowTestEvent : IDomainEvent
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

    private sealed class SlowTestDomainEventHandler(EventProcessingOutput eventProcessingOutput) : IDomainEventHandler<SlowTestEvent>
    {
        public async Task HandleDomainEventAsync(SlowTestEvent domainEvent, CancellationToken cancellationToken)
        {
            eventProcessingOutput.StartProcessing();
            await Task.Delay(SlowHandlerDelay, cancellationToken);
            eventProcessingOutput.IncrementCount();
        }
    }

    private sealed class EventGridEmulatorContext(IContainer container) : IAsyncDisposable
    {
        public string Url { get; } = $"http://localhost:{container.GetMappedPublicPort(EmulatorPort)}/";

        public static async Task<EventGridEmulatorContext> StartAsync(ITestOutputHelper testOutputHelper)
        {
            var configurationPath = await WriteConfigurationFile(testOutputHelper);
            
            var container = BuildContainer(configurationPath);
            
            try
            {
                await container.StartAsync();
            }
            catch
            {
                try
                {
                    var logs = await container.GetLogsAsync();
                    testOutputHelper.WriteLine(logs.Stdout);
                    testOutputHelper.WriteLine(logs.Stderr);
                }
                catch
                {
                    // do not hide the original exception
                }

                throw;
            }

            return new EventGridEmulatorContext(container);
        }

        private static IContainer BuildContainer(string configurationPath)
        {
            return new ContainerBuilder()
                .WithImage("workleap/eventgridemulator:0.2.0")
                .WithPortBinding(EmulatorPort, assignRandomHostPort: true)
                .WithBindMount(configurationPath, "/app/appsettings.json", AccessMode.ReadOnly)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(EmulatorPort))
                .Build();
        }

        private static async Task<string> WriteConfigurationFile(ITestOutputHelper testOutputHelper)
        {
            var path = Path.GetTempFileName();
            await File.WriteAllTextAsync(path, GetConfiguration());
            if (!OperatingSystem.IsWindows())
            {
                await CliWrap.Cli.Wrap("chmod").WithArguments(["0444", path]).ExecuteAsync();
            }
            
            // For debug purposes only
            testOutputHelper.WriteLine("Write configuration file at: " + path);
            testOutputHelper.WriteLine("Write configuration content: " + await File.ReadAllTextAsync(path));

            return path;
        }

        private static string GetConfiguration()
        {
            return $$"""
                     {
                       "Topics": {
                         "{{TopicName}}": [
                           "pull://{{SubscriberName}}"
                         ]
                       }
                     }
                     """;
        }
        
        public async ValueTask DisposeAsync()
        {
            await container.DisposeAsync();
        }
    }

    private sealed class EventProcessingOutput
    {
        private readonly object _lock = new();

        private readonly TaskCompletionSource _taskCompletionSource = new();

        private Stopwatch? _stopWatch;

        public int Count { get; private set; }

        public int Total { get; private set; }

        public TimeSpan Duration { get; private set; }

        public decimal EventsPerSecond => this.Count / (decimal)this.Duration.TotalSeconds;

        public void IncrementCount()
        {
            lock (this._lock)
            {
                this.Count++;

                if (this.Count == this.Total)
                {
                    this.Duration = this._stopWatch?.Elapsed ?? TimeSpan.Zero;
                    this._taskCompletionSource.TrySetResult();
                }
            }
        }

        public void IncrementTotal(int incrementSize)
        {
            lock (this._lock)
            {
                this.Total += incrementSize;
            }
        }

        public void StartProcessing()
        {
            this._stopWatch ??= Stopwatch.StartNew();
        }

        public Task WaitForCompletion(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => this._taskCompletionSource.TrySetCanceled(cancellationToken));
            return this._taskCompletionSource.Task;
        }
    }
}