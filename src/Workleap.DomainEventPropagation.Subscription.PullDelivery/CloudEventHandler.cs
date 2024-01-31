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
        this._pipeline = domainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)HandleDomainEventAsync, BuildPipeline);
    }

    public async Task<EventProcessingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var domainEventWrapper = new DomainEventWrapper(cloudEvent);

        if (this.GetDomainEventType(domainEventWrapper!.DomainEventName) == null)
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

    private static Task<EventProcessingStatus> HandleDomainEventAsync(
        DomainEventWrapper domainEventWrapper,
        CancellationToken cancellationToken)
    {
        // Todo : Get event handler that matches wrapper type and invoke it
        return Task.FromResult(EventProcessingStatus.Handled);
    }
}