using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal class EventPuller : BackgroundService
{
    private readonly ILogger<EventPuller> _logger;
    private readonly EventGridReceptionChannel[] _eventGridReceptionChannels;

    public EventPuller(
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IAzureClientFactory<EventGridClient> eventGridClientFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPuller> logger)
    {
        this._logger = logger;
        this._eventGridReceptionChannels = clientDescriptors.Select(descriptor =>
            new EventGridReceptionChannel(
                optionsMonitor.Get(descriptor.Name).TopicName,
                optionsMonitor.Get(descriptor.Name).SubscriptionName,
                eventGridClientFactory.CreateClient(descriptor.Name))).ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridReceptionChannels.Select(channel => Task.Run(() => StartReceivingEventsAsync(channel, this._logger, stoppingToken), stoppingToken)));
    }

    private static async Task StartReceivingEventsAsync(EventGridReceptionChannel channel, ILogger<EventPuller> logger, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await channel.Client.ReceiveCloudEventsAsync(channel.Topic, channel.Subscription, cancellationToken: stoppingToken).ConfigureAwait(false);
                foreach (var detail in result.Value.Value)
                {
                    var cloudEvent = detail.Event;
                    var lockToken = detail.BrokerProperties.LockToken;

                    // TODO : Handle event and pass token + client to handler so that we can acknowledge the event
                }
            }
            catch (Exception e)
            {
                logger.CloudEventCannotBeReceived(channel.Topic, channel.Subscription, e.Message);
            }
        }
    }

    private record EventGridReceptionChannel(string Topic, string Subscription, EventGridClient Client);
}