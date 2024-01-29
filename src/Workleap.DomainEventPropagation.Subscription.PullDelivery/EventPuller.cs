using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal class EventPuller : BackgroundService
{
    private readonly ILogger<EventPuller> _logger;
    private readonly EventGridSubscription[] _eventGridSubscriptions;

    public EventPuller(
        IServiceProvider services,
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPuller> logger)
    {
        this._logger = logger;
        this._eventGridSubscriptions = clientDescriptors.Select(descriptor =>
            new EventGridSubscription(
                services.GetRequiredKeyedService<ISubscriptionHandler>(descriptor.Name),
                eventGridClientWrapperFactory.CreateClient(descriptor.Name),
                optionsMonitor.Get(descriptor.Name).TopicName,
                optionsMonitor.Get(descriptor.Name).SubscriptionName)).ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridSubscriptions.Select(channel => Task.Run(() => StartReceivingEventsAsync(channel, this._logger, stoppingToken), stoppingToken)));
    }

    private static async Task StartReceivingEventsAsync(EventGridSubscription subscription, ILogger<EventPuller> logger, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bundles = await subscription.Client.ReceiveCloudEventsAsync(subscription.TopicName, subscription.SubscriptionName, cancellationToken: stoppingToken).ConfigureAwait(false);
                foreach (var (cloudEvent, lockToken) in bundles)
                {
                    var status = await subscription.Handler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
                    switch (status)
                    {
                        case EventProcessingStatus.Handled:
                            await AcknowledgeEvent(subscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Released:
                            await ReleaseEvent(subscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Rejected:
                            await RejectEvent(subscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        default:
                            throw new NotSupportedException($"{status} is not a supported {nameof(EventProcessingStatus)}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.CloudEventCannotBeReceived(subscription.TopicName, subscription.SubscriptionName, e.Message);
            }
        }
    }

    private static Task AcknowledgeEvent(EventGridSubscription channel, string lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.AcknowledgeCloudEventAsync(channel.TopicName, channel.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task ReleaseEvent(EventGridSubscription channel, string lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.ReleaseCloudEventAsync(channel.TopicName, channel.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task RejectEvent(EventGridSubscription channel, string lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.RejectCloudEventAsync(channel.TopicName, channel.SubscriptionName, lockToken, stoppingToken);
    }

    private record EventGridSubscription(ISubscriptionHandler Handler, IEventGridClientAdapter Client, string TopicName, string SubscriptionName);
}