﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal class EventPuller : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EventPuller> _logger;
    private readonly EventGridTopicSubscription[] _eventGridTopicSubscriptions;

    public EventPuller(
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPuller> logger)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._logger = logger;
        this._eventGridTopicSubscriptions = clientDescriptors.Select(descriptor =>
            new EventGridTopicSubscription(
                optionsMonitor.Get(descriptor.Name).TopicName,
                optionsMonitor.Get(descriptor.Name).SubscriptionName,
                eventGridClientWrapperFactory.CreateClient(descriptor.Name))).ToArray();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(this._eventGridTopicSubscriptions.Select(sub => Task.Run(() => this.StartReceivingEventsAsync(sub, this._logger, stoppingToken), stoppingToken)));
    }

    private async Task StartReceivingEventsAsync(EventGridTopicSubscription eventGridTopicSubscription, ILogger<EventPuller> logger, CancellationToken stoppingToken)
    {
        using var scope = this._serviceScopeFactory.CreateScope();
        var cloudEventHandler = scope.ServiceProvider.GetRequiredService<ICloudEventHandler>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bundles = await eventGridTopicSubscription.Client.ReceiveCloudEventsAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, cancellationToken: stoppingToken).ConfigureAwait(false);
                foreach (var (cloudEvent, lockToken) in bundles)
                {
                    var status = await cloudEventHandler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
                    switch (status)
                    {
                        case EventProcessingStatus.Handled:
                            await AcknowledgeEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Released:
                            await ReleaseEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        case EventProcessingStatus.Rejected:
                            await RejectEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                            break;
                        default:
                            throw new NotSupportedException($"{status} is not a supported {nameof(EventProcessingStatus)}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.CloudEventCannotBeReceived(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, e.Message);
            }
        }
    }

    private static Task AcknowledgeEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        return eventGridTopicSubscription.Client.AcknowledgeCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task ReleaseEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        return eventGridTopicSubscription.Client.ReleaseCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken);
    }

    private static Task RejectEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        return eventGridTopicSubscription.Client.RejectCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken);
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, IEventGridClientAdapter Client);
}