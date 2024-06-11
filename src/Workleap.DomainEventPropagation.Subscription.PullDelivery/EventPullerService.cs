using System.Threading.Channels;
using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal class EventPullerService : BackgroundService
{
    private readonly List<EventGridSubscriptionEventPuller> _eventGridSubscriptionPullers;

    public EventPullerService(
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPullerService> logger)
    {
        this._eventGridSubscriptionPullers = clientDescriptors.Select(descriptor =>
                new EventGridSubscriptionEventPuller(
                    new EventGridTopicSubscription(
                        optionsMonitor.Get(descriptor.Name).TopicName,
                        optionsMonitor.Get(descriptor.Name).SubscriptionName,
                        optionsMonitor.Get(descriptor.Name).MaxDop,
                        eventGridClientWrapperFactory.CreateClient(descriptor.Name)),
                    serviceScopeFactory,
                    logger))
            .ToList();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridSubscriptionPullers.Select(puller => Task.Run(() => puller.StartReceivingEventsAsync(stoppingToken), stoppingToken)));
    }

    private class EventGridSubscriptionEventPuller
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventPullerService> _logger;
        private readonly EventGridTopicSubscription _eventGridTopicSubscription;

        private readonly Channel<Task<EventHandlingResult>> _handlerTasksChannel;
        private readonly Channel<string> _acknowledgeEventChannel = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _releaseEventChannel = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _rejectEventChannel = Channel.CreateUnbounded<string>();

        public EventGridSubscriptionEventPuller(
            EventGridTopicSubscription eventGridTopicSubscription,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EventPullerService> logger)
        {
            this._serviceScopeFactory = serviceScopeFactory;
            this._logger = logger;
            this._eventGridTopicSubscription = eventGridTopicSubscription;

            this._handlerTasksChannel = new TaskBoundedChannel<Task<EventHandlingResult>>(this._eventGridTopicSubscription.MaxHandlerDop);
        }

        public Task StartReceivingEventsAsync(CancellationToken cancellationToken)
        {
            // Start the tasks that will feed events into the handler tasks channel and handle the completed event handlers
            return Task.WhenAll(
                this.FeedEventsIntoChannel(cancellationToken),
                this.HandleCompletedEventHandlers(cancellationToken),
                this.AcknowledgeEvents(cancellationToken),
                this.ReleaseEvents(cancellationToken),
                this.RejectEvents(cancellationToken));
        }

        private async Task FeedEventsIntoChannel(CancellationToken cancellationToken)
        {
            await using var scope = this._serviceScopeFactory.CreateAsyncScope();
            var cloudEventHandler = scope.ServiceProvider.GetRequiredService<ICloudEventHandler>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await this._handlerTasksChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false);

                    var bundles = await this._eventGridTopicSubscription.Client.ReceiveCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, this._eventGridTopicSubscription.MaxHandlerDop - this._handlerTasksChannel.Reader.Count, cancellationToken).ConfigureAwait(false);

                    foreach (var bundle in bundles)
                    {
                        await this._handlerTasksChannel.Writer.WriteAsync(HandleEventAsync(cloudEventHandler, bundle.Event, bundle.LockToken, cancellationToken), cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.CloudEventCouldNotBeHandled(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, ex);
                }
            }
        }

        private static async Task<EventHandlingResult> HandleEventAsync(ICloudEventHandler cloudEventHandler, CloudEvent cloudEvent, string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                await cloudEventHandler.HandleCloudEventAsync(cloudEvent, cancellationToken).ConfigureAwait(false);
                return new EventHandlingResult(cloudEvent, lockToken, null);
            }
            catch (Exception ex)
            {
                return new EventHandlingResult(cloudEvent, lockToken, ex);
            }
        }

        private async Task HandleCompletedEventHandlers(CancellationToken cancellationToken)
        {
            // Read from the handler task channel and write to the appropriate channel based on the result
            await foreach (var handlerTask in this._handlerTasksChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var handlerResult = await handlerTask.ConfigureAwait(false);

                switch (handlerResult.Exception)
                {
                    case null:
                        await this._acknowledgeEventChannel.Writer.WriteAsync(handlerResult.LockToken, cancellationToken).ConfigureAwait(false);
                        break;
                    case DomainEventTypeNotRegisteredException:
                    case CloudEventSerializationException:
                    case DomainEventHandlerNotRegisteredException:
                        this._logger.EventWillBeRejected(handlerResult.CloudEvent.Id, handlerResult.CloudEvent.Type, handlerResult.Exception);
                        await this._rejectEventChannel.Writer.WriteAsync(handlerResult.LockToken, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        this._logger.EventWillBeReleased(handlerResult.CloudEvent.Id, handlerResult.CloudEvent.Type, handlerResult.Exception);
                        await this._releaseEventChannel.Writer.WriteAsync(handlerResult.LockToken, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async Task AcknowledgeEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var lockTokens = await ReadCurrentChannelContent(this._acknowledgeEventChannel, cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.AcknowledgeCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, lockTokens, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReleaseEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var lockTokens = await ReadCurrentChannelContent(this._releaseEventChannel, cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.ReleaseCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, lockTokens, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RejectEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var lockTokens = await ReadCurrentChannelContent(this._rejectEventChannel, cancellationToken).ConfigureAwait(false);
                await this._eventGridTopicSubscription.Client.RejectCloudEventsAsync(this._eventGridTopicSubscription.TopicName, this._eventGridTopicSubscription.SubscriptionName, lockTokens, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<List<string>> ReadCurrentChannelContent(Channel<string> channel, CancellationToken cancellationToken)
        {
            await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);

            var currentChannelCount = channel.Reader.Count;
            var results = new List<string>();

            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                results.Add(result);

                if (results.Count == currentChannelCount)
                {
                    break;
                }
            }

            return results;
        }
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, int MaxHandlerDop, IEventGridClientAdapter Client);
    
    private record EventHandlingResult(CloudEvent CloudEvent, string LockToken, Exception? Exception);
}