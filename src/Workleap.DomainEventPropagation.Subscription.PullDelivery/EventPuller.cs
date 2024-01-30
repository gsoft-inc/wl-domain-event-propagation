using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal class EventPuller : BackgroundService
{
    private readonly ICloudEventHandler _cloudEventHandler;
    private readonly ILogger<EventPuller> _logger;
    private readonly EventGridTopicSubscription[] _eventGridTopicSubscriptions;

    public EventPuller(
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        ICloudEventHandler cloudEventHandler,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPuller> logger)
    {
        this._logger = logger;
        this._cloudEventHandler = cloudEventHandler;
        this._eventGridTopicSubscriptions = clientDescriptors.Select(descriptor =>
            new EventGridTopicSubscription(
                optionsMonitor.Get(descriptor.Name).TopicName,
                optionsMonitor.Get(descriptor.Name).SubscriptionName,
                eventGridClientWrapperFactory.CreateClient(descriptor.Name))).ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridTopicSubscriptions.Select(channel => Task.Run(() => this.StartReceivingEventsAsync(channel, this._logger, stoppingToken), stoppingToken)));
    }

    private async Task StartReceivingEventsAsync(EventGridTopicSubscription topicSub, ILogger<EventPuller> logger, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bundles = await topicSub.Client.ReceiveCloudEventsAsync(topicSub.TopicName, topicSub.SubscriptionName, cancellationToken: stoppingToken).ConfigureAwait(false);
                foreach (var (cloudEvent, lockToken) in bundles)
                {
                    var status = await this._cloudEventHandler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
                    switch (status)
                    {
                        case EventProcessingStatus.Handled:
                            await AcknowledgeEvent(topicSub, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Released:
                            await ReleaseEvent(topicSub, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Rejected:
                            await RejectEvent(topicSub, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        default:
                            throw new NotSupportedException($"{status} is not a supported {nameof(EventProcessingStatus)}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.CloudEventCannotBeReceived(topicSub.TopicName, topicSub.SubscriptionName, e.Message);
            }
        }
    }

    private static Task AcknowledgeEvent(EventGridTopicSubscription topicSub, string lockToken, CancellationToken stoppingToken)
    {
        return topicSub.Client.AcknowledgeCloudEventAsync(topicSub.TopicName, topicSub.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task ReleaseEvent(EventGridTopicSubscription topicSub, string lockToken, CancellationToken stoppingToken)
    {
        return topicSub.Client.ReleaseCloudEventAsync(topicSub.TopicName, topicSub.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task RejectEvent(EventGridTopicSubscription topicSub, string lockToken, CancellationToken stoppingToken)
    {
        return topicSub.Client.RejectCloudEventAsync(topicSub.TopicName, topicSub.SubscriptionName, lockToken, stoppingToken);
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, IEventGridClientAdapter Client);
}