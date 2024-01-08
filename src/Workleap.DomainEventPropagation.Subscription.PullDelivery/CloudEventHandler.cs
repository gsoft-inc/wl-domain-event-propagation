using Azure.Messaging;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal sealed class CloudEventHandler : BaseEventHandler, ICloudEventHandler
{
    private readonly ILogger<CloudEventHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public CloudEventHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        IEnumerable<IDomainEventBehavior> domainEventBehaviors,
        ILogger<CloudEventHandler> logger)
        : base(serviceProvider, domainEventTypeRegistry)
    {
        this._logger = logger;
        this._pipeline = domainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)this.HandleDomainEventAsync, BuildPipeline);
    }

    public async Task<EventProcessingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var domainEventWrapper = new DomainEventWrapper(cloudEvent);

        if (this.IsDomainEventRegistrationMissing(domainEventWrapper.DomainEventName))
        {
            this._logger.EventDomainTypeNotRegistered(domainEventWrapper.DomainEventName, cloudEvent.Subject ?? "Unknown");
            return EventProcessingStatus.Rejected;
        }

        return await this._pipeline(domainEventWrapper, cancellationToken).ConfigureAwait(false);
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, IDomainEventBehavior behavior)
    {
        return (@event, cancellationToken) => behavior.HandleAsync(@event, next, cancellationToken);
    }

    private async Task<EventProcessingStatus> HandleDomainEventAsync(
        DomainEventWrapper domainEventWrapper,
        CancellationToken cancellationToken)
    {
        var handler = this.BuildDomainEventHandler(domainEventWrapper, cancellationToken);
        if (handler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(domainEventWrapper.DomainEventName);
            return EventProcessingStatus.Released;
        }

        try
        {
            await handler().ConfigureAwait(false);
            return EventProcessingStatus.Handled;
        }
        catch (Exception e)
        {
            this._logger.EventHandlingFailed(domainEventWrapper.DomainEventName, e.Message);
            return EventProcessingStatus.Rejected;
        }
    }
}