using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation;

internal class EventPullerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EventPullerService> _logger;
    private readonly EventGridTopicSubscription[] _eventGridTopicSubscriptions;

    public EventPullerService(
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<EventGridClientDescriptor> clientDescriptors,
        IEventGridClientWrapperFactory eventGridClientWrapperFactory,
        IOptionsMonitor<EventPropagationSubscriptionOptions> optionsMonitor,
        ILogger<EventPullerService> logger)
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

    private async Task StartReceivingEventsAsync(EventGridTopicSubscription eventGridTopicSubscription, ILogger<EventPullerService> logger, CancellationToken stoppingToken)
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
                    await this.HandleEventAsync(cloudEventHandler, eventGridTopicSubscription, cloudEvent, lockToken, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this._logger.CloudEventCannotBeReceived(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, ex);
            }
        }
    }

    private async Task HandleEventAsync(ICloudEventHandler cloudEventHandler, EventGridTopicSubscription eventGridTopicSubscription, CloudEvent cloudEvent, string lockToken, CancellationToken stoppingToken)
    {
        try
        {
            await cloudEventHandler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case DomainEventTypeNotRegisteredException:
                case CloudEventSerializationException:
                case DomainEventHandlerNotRegisteredException:
                    this._logger.EventWillBeRejected(cloudEvent.Id, cloudEvent.Type, ex);
                    await this.RejectEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                    return;
                default:
                    this._logger.EventWillBeReleased(cloudEvent.Id, cloudEvent.Type, ex);
                    await this.ReleaseEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                    return;
            }
        }
        
        await this.AcknowledgeEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
    }

    private async Task AcknowledgeEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        try
        {
            await eventGridTopicSubscription.Client.AcknowledgeCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.CloudEventCannotBeAcknowledged(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, ex);
        }
    }

    private async Task ReleaseEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        try
        {
            await eventGridTopicSubscription.Client.ReleaseCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.CloudEventCannotBeReleased(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, ex);
        }
    }

    private async Task RejectEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        try
        {
            await eventGridTopicSubscription.Client.RejectCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.CloudEventCannotBeRejected(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, ex);
        }
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, IEventGridClientAdapter Client);
}