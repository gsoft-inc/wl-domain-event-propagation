using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.ClientWrapper;

namespace Workleap.DomainEventPropagation;

internal class EventPuller : BackgroundService
{
    private readonly ICloudEventHandler _cloudEventHandler;
    private readonly ILogger<EventPuller> _logger;
    private readonly EventGridReceptionChannel[] _eventGridReceptionChannels;

    public EventPuller(
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        ICloudEventHandler cloudEventHandler,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPuller> logger)
    {
        this._logger = logger;
        this._cloudEventHandler = cloudEventHandler;
        this._eventGridReceptionChannels = clientDescriptors.Select(descriptor =>
            new EventGridReceptionChannel(
                optionsMonitor.Get(descriptor.Name).TopicName,
                optionsMonitor.Get(descriptor.Name).SubscriptionName,
                eventGridClientWrapperFactory.CreateClient(descriptor.Name))).ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridReceptionChannels.Select(channel => Task.Run(() => this.StartReceivingEventsAsync(channel, this._logger, stoppingToken), stoppingToken)));
    }

    private async Task StartReceivingEventsAsync(EventGridReceptionChannel channel, ILogger<EventPuller> logger, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bundles = await channel.Client.ReceiveCloudEventsAsync(channel.Topic, channel.Subscription, cancellationToken: stoppingToken).ConfigureAwait(false);
                foreach (var (cloudEvent, lockToken) in bundles)
                {
                    var status = await this._cloudEventHandler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
                    switch (status)
                    {
                        case HandlingStatus.Handled:
                            await AcknowledgeEvent(channel, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case HandlingStatus.Released:
                            await ReleaseEvent(channel, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case HandlingStatus.Rejected:
                            await RejectEvent(channel, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        default:
                            throw new NotSupportedException($"{status} is not a supported {nameof(HandlingStatus)}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.CloudEventCannotBeReceived(channel.Topic, channel.Subscription, e.Message);
            }
        }
    }

    private static Task AcknowledgeEvent(EventGridReceptionChannel channel, string? lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.AcknowledgeCloudEventsAsync(channel.Topic, channel.Subscription, new AcknowledgeOptions(new[] { lockToken }), stoppingToken);
    }

    private static Task ReleaseEvent(EventGridReceptionChannel channel, string? lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.ReleaseCloudEventsAsync(channel.Topic, channel.Subscription, new ReleaseOptions(new[] { lockToken }), stoppingToken);
    }

    private static Task RejectEvent(EventGridReceptionChannel channel, string? lockToken, CancellationToken stoppingToken)
    {
        return channel.Client.RejectCloudEventsAsync(channel.Topic, channel.Subscription, new RejectOptions(new[] { lockToken }), stoppingToken);
    }

    private record EventGridReceptionChannel(string Topic, string Subscription, EventGridClientWrapper Client);
}