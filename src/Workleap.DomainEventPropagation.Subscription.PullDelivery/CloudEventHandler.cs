using Azure.Messaging;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal class CloudEventHandler : ICloudEventHandler
{
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;
    private readonly ILogger<CloudEventHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public CloudEventHandler(
        IDomainEventTypeRegistry domainEventTypeRegistry,
        IEnumerable<IDomainEventBehavior> domainEventBehaviors,
        ILogger<CloudEventHandler> logger)
    {
        this._domainEventTypeRegistry = domainEventTypeRegistry;
        this._logger = logger;
        this._pipeline = domainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)HandleDomainEventAsync, BuildPipeline);
    }

    public async Task<HandlingStatus> HandleCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var domainEventWrapper = new DomainEventWrapper(cloudEvent);

        var domainEventType = this._domainEventTypeRegistry.GetDomainEventType(domainEventWrapper.DomainEventName);
        if (domainEventType == null)
        {
            this._logger.EventDomainTypeNotRegistered(domainEventWrapper.DomainEventName, cloudEvent.Subject ?? "Unknown");
            return HandlingStatus.Rejected;
        }

        return await this._pipeline(domainEventWrapper, cancellationToken).ConfigureAwait(false);
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, IDomainEventBehavior pipeline)
    {
        return (@event, cancellationToken) => pipeline.HandleAsync(@event, next, cancellationToken);
    }

    private static Task<HandlingStatus> HandleDomainEventAsync(
        DomainEventWrapper domainEventWrapper,
        CancellationToken cancellationToken)
    {
        // Todo : Get event handler that matches wrapper type and invoke it
        return Task.FromResult(HandlingStatus.Handled);
    }
}