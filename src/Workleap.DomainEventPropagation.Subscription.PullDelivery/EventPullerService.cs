using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPullerService : BackgroundService
{
    private readonly List<EventGridSubscriptionEventPuller> _eventGridSubscriptionPullers;

    // For testing purposes    
    private readonly Dictionary<string, SemaphoreSlim>? _processEventSemaphoreSlim;
    private TaskCompletionSource? _eventProcessedTaskCompletionSource;

    public EventPullerService(
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        IOptions<EventPullerServiceOptions> serviceOptions,
        ILogger<EventPullerService> logger)
    {
        var clientDescriptorsList = clientDescriptors.ToArray();

        this.ProcessEventManually = serviceOptions.Value.ProcessEventManually;
        this._eventGridSubscriptionPullers = clientDescriptorsList
            .Select(descriptor =>
            {
                var eventPropagationSubscriptionOptions = optionsMonitor.Get(descriptor.Name);
                return new EventGridSubscriptionEventPuller(
                            this,
                            descriptor.Name,
                            new EventGridTopicSubscription(
                                eventPropagationSubscriptionOptions.TopicName,
                                eventPropagationSubscriptionOptions.SubscriptionName,
                                eventPropagationSubscriptionOptions.MaxDegreeOfParallelism,
                                eventGridClientWrapperFactory.CreateClient(descriptor.Name)),
                            serviceScopeFactory,
                            logger);
            })
            .ToList();

        if (this.ProcessEventManually)
        {
            this._processEventSemaphoreSlim = new();
            foreach (var descriptor in clientDescriptorsList)
            {
                this._processEventSemaphoreSlim.Add(descriptor.Name, new SemaphoreSlim(initialCount: 0, maxCount: 1));
            }
        }
    }

    [MemberNotNullWhen(true, nameof(_processEventSemaphoreSlim))]
    private bool ProcessEventManually { get; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridSubscriptionPullers.Select(puller => Task.Run(() => puller.StartReceivingEventsAsync(stoppingToken), stoppingToken)));
    }

    // For testing purposes
    // note: this method is not thread-safe. Support only one caller at a time.
    internal async Task ProcessOneEventAsync(string? descriptorName = null)
    {
        if (!this.ProcessEventManually)
        {
            throw new InvalidOperationException("Debug mode is not enabled");
        }

        this._eventProcessedTaskCompletionSource = new TaskCompletionSource();

        if (descriptorName is null)
        {
            foreach (var kvp in this._processEventSemaphoreSlim)
            {
                kvp.Value.Release();
                await this._eventProcessedTaskCompletionSource.Task.ConfigureAwait(false);
                this._eventProcessedTaskCompletionSource = new TaskCompletionSource();
            }
        }
        else
        {
            this._processEventSemaphoreSlim[descriptorName].Release();
            await this._eventProcessedTaskCompletionSource.Task.ConfigureAwait(false);
        }

        this._eventProcessedTaskCompletionSource = null;
    }

    public override void Dispose()
    {
        if (this._processEventSemaphoreSlim is not null)
        {
            foreach (var kvp in this._processEventSemaphoreSlim)
            {
                kvp.Value.Dispose();
            }
        }

        base.Dispose();
    }

    private class EventGridSubscriptionEventPuller
    {
        private const int OutputChannelSize = 5000;
        private const int MaxEventRequestSize = 100;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventPullerService> _logger;
        private readonly EventPullerService _pullerService;
        private readonly string _descriptorName;
        private readonly EventGridTopicSubscription _eventGridTopicSubscription;

        private readonly Channel<string> _acknowledgeEventChannel = Channel.CreateBounded<string>(OutputChannelSize);
        private readonly Channel<string> _releaseEventChannel = Channel.CreateBounded<string>(OutputChannelSize);
        private readonly Channel<string> _rejectEventChannel = Channel.CreateBounded<string>(OutputChannelSize);

        private int _handlersInProgress;
        private TaskCompletionSource _taskCompletionSource = new();

        public EventGridSubscriptionEventPuller(
            EventPullerService pullerService,
            string descriptorName,
            EventGridTopicSubscription eventGridTopicSubscription,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EventPullerService> logger)
        {
            this._serviceScopeFactory = serviceScopeFactory;
            this._logger = logger;
            this._pullerService = pullerService;
            this._descriptorName = descriptorName;
            this._eventGridTopicSubscription = eventGridTopicSubscription;
        }

        public Task StartReceivingEventsAsync(CancellationToken cancellationToken)
        {
            // Start the tasks that will process events and handle the completion callback
            return Task.WhenAll(
                this.ProcessEvents(cancellationToken),
                this.AcknowledgeEvents(cancellationToken),
                this.ReleaseEvents(cancellationToken),
                this.RejectEvents(cancellationToken));
        }

        private Task ProcessEvents(CancellationToken cancellationToken)
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = this._eventGridTopicSubscription.MaxHandlerDop, CancellationToken = cancellationToken };

            return Parallel.ForEachAsync(this.StreamEventGridEvents(cancellationToken), parallelOptions, async (bundle, cancellationToken) =>
            {
                if (this._pullerService.ProcessEventManually)
                {
                    await this._pullerService._processEventSemaphoreSlim![this._descriptorName].WaitAsync(cancellationToken).ConfigureAwait(false);
                }

                this.SignalHandlerStarting();

                try
                {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                    await using var scope = this._serviceScopeFactory.CreateAsyncScope();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                    var cloudEventHandler = scope.ServiceProvider.GetRequiredService<ICloudEventHandler>();

                    await this.HandleBundleAsync(cloudEventHandler, bundle.Event, bundle.LockToken, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    this.SignalHandlerCompleted();
                    this._pullerService._eventProcessedTaskCompletionSource?.SetResult();
                }
            });
        }

        private async IAsyncEnumerable<EventGridClientAdapter.EventGridClientAdapter.EventBundle> StreamEventGridEvents([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this.WaitForAvailableHandler(cancellationToken).ConfigureAwait(false);

                var availableHandlers = this._eventGridTopicSubscription.MaxHandlerDop - this._handlersInProgress;
                if (availableHandlers <= 0)
                {
                    continue;
                }

                var bundles = await this._eventGridTopicSubscription.Client.ReceiveCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, Math.Min(availableHandlers, MaxEventRequestSize), cancellationToken).ConfigureAwait(false);

                foreach (var bundle in bundles)
                {
                    yield return bundle;
                }
            }
        }

        private async Task HandleBundleAsync(ICloudEventHandler cloudEventHandler, CloudEvent cloudEvent, string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                await cloudEventHandler.HandleCloudEventAsync(cloudEvent, cancellationToken).ConfigureAwait(false);
                await this._acknowledgeEventChannel.Writer.WriteAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case DomainEventTypeNotRegisteredException:
                    case CloudEventSerializationException:
                    case DomainEventHandlerNotRegisteredException:
                        this._logger.EventWillBeRejected(cloudEvent.Id, cloudEvent.Type, ex);
                        await this._rejectEventChannel.Writer.WriteAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        this._logger.EventWillBeReleased(cloudEvent.Id, cloudEvent.Type, ex);
                        await this._releaseEventChannel.Writer.WriteAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async Task AcknowledgeEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this._acknowledgeEventChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.AcknowledgeCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, ReadCurrentContent(this._acknowledgeEventChannel), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReleaseEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this._releaseEventChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.ReleaseCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, ReadCurrentContent(this._releaseEventChannel), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RejectEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this._rejectEventChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.RejectCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, ReadCurrentContent(this._rejectEventChannel), cancellationToken).ConfigureAwait(false);
            }
        }

        private void SignalHandlerStarting()
        {
            Interlocked.Increment(ref this._handlersInProgress);
        }

        private void SignalHandlerCompleted()
        {
            Interlocked.Decrement(ref this._handlersInProgress);
            lock (this._eventGridTopicSubscription)
            {
                this._taskCompletionSource.TrySetResult();
                this._taskCompletionSource = new TaskCompletionSource();
            }
        }

        private ValueTask WaitForAvailableHandler(CancellationToken cancellationToken)
        {
            lock (this._eventGridTopicSubscription)
            {
                if (this._handlersInProgress < this._eventGridTopicSubscription.MaxHandlerDop)
                {
                    return ValueTask.CompletedTask;
                }

                return new ValueTask(this._taskCompletionSource.Task.WaitAsync(cancellationToken));
            }
        }

        private static IEnumerable<string> ReadCurrentContent(Channel<string> channel)
        {
            var maxResultCount = channel.Reader.Count;
            var resultCounter = 0;

            while (channel.Reader.TryRead(out var result))
            {
                yield return result;

                if (++resultCounter >= maxResultCount)
                {
                    break;
                }
            }
        }
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, int MaxHandlerDop, IEventGridClientAdapter Client);
}