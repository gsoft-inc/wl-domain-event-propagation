using Azure.Messaging;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal sealed class CloudEventHandler : BaseEventHandler, ICloudEventHandler
{
    private readonly ILogger<ICloudEventHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public CloudEventHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        IEnumerable<IDomainEventBehavior> domainEventBehaviors,
        ILogger<ICloudEventHandler> logger)
        : base(serviceProvider, domainEventTypeRegistry)
    {
        this._logger = logger;
        this._pipeline = domainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)this.HandleDomainEventAsync, BuildPipeline);
    }

    public async Task<EventProcessingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryWrapCloudEvent(cloudEvent, out var domainEventWrapper))
            {
                this._logger.IllFormedCloudEvent(cloudEvent.Id);
                return EventProcessingStatus.Rejected;
            }

            if (this.GetDomainEventType(domainEventWrapper!.DomainEventName) == null)
            {
                this._logger.EventDomainTypeNotRegistered(domainEventWrapper.DomainEventName, cloudEvent.Subject ?? "Unknown");
                return EventProcessingStatus.Rejected;
            }

            return await this._pipeline(domainEventWrapper, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            this._logger.EventHandlingFailed(cloudEvent.Type, cloudEvent.Id, e.Message);
            return EventProcessingStatus.Released;
        }
    }

    private static bool TryWrapCloudEvent(CloudEvent cloudEvent, out DomainEventWrapper? domainEventWrapper)
    {
        domainEventWrapper = null;
        try
        {
            domainEventWrapper = new DomainEventWrapper(cloudEvent);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, IDomainEventBehavior behavior)
    {
        return (@event, cancellationToken) => behavior.HandleAsync(@event, next, cancellationToken);
    }

    private async Task<EventProcessingStatus> HandleDomainEventAsync(
        DomainEventWrapper domainEventWrapper,
        CancellationToken cancellationToken)
    {
        var handler = this.BuildHandleDomainEventAsyncMethod(domainEventWrapper, cancellationToken);

        if (handler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(domainEventWrapper.DomainEventName);
            return EventProcessingStatus.Rejected;
        }

        await handler().ConfigureAwait(false);
        return EventProcessingStatus.Handled;
    }
}