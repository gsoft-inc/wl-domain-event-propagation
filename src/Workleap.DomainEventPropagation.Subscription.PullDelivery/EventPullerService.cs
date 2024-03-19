using System.ComponentModel.Design;
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
            var bundles = await eventGridTopicSubscription.Client.ReceiveCloudEventsAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, cancellationToken: stoppingToken).ConfigureAwait(false);
            foreach (var (cloudEvent, lockToken) in bundles)
            {
                await this.HandleEventAsync(cloudEventHandler, eventGridTopicSubscription, cloudEvent, lockToken, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleEventAsync(ICloudEventHandler cloudEventHandler, EventGridTopicSubscription eventGridTopicSubscription, CloudEvent cloudEvent, string lockToken, CancellationToken stoppingToken)
    {
        try
        {
            throw new Exception();
            await cloudEventHandler.HandleCloudEventAsync(cloudEvent, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The first 3 use cases --> 3 excepted use cases --> handle and log and thats it. Those are the expected errors. --> no need to rethrow the exceptions here.
            switch (ex)
            {
                case DomainEventTypeNotRegisteredException:
                case CloudEventSerializationException:
                case DomainEventHandlerNotRegisteredException:
                    this._logger.EventWillBeRejected(cloudEvent.Id, cloudEvent.Type, ex);
                    await RejectEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                    break;

                // If we just catch the exception, we hide the exception here --> will never see the event. This is an unexpected error.
                default:
                    await ReleaseEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
                    throw;
            }
        }

        await AcknowledgeEvent(eventGridTopicSubscription, lockToken, stoppingToken).ConfigureAwait(false);
    }

    private static async Task AcknowledgeEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    { 
        await eventGridTopicSubscription.Client.AcknowledgeCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
    }

    private static async Task ReleaseEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        await eventGridTopicSubscription.Client.ReleaseCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
    }

    private static async Task RejectEvent(EventGridTopicSubscription eventGridTopicSubscription, string lockToken, CancellationToken stoppingToken)
    {
        await eventGridTopicSubscription.Client.RejectCloudEventAsync(eventGridTopicSubscription.TopicName, eventGridTopicSubscription.SubscriptionName, lockToken, stoppingToken).ConfigureAwait(false);
    }

    private record EventGridTopicSubscription(string TopicName, string SubscriptionName, IEventGridClientAdapter Client);
}