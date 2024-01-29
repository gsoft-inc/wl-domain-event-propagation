using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal class SubscriptionHandler : BaseEventHandler, ISubscriptionHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGlobalDomainEventTypeRegistry _globalDomainEventTypeRegistry;
    private readonly IKeyedDomainEventTypeRegistry _keyedDomainEventTypeRegistry;
    private readonly string _handlerKey;

    private readonly ILogger<ISubscriptionHandler> _logger;

    private readonly DomainEventHandlerDelegate _pipeline;

    public SubscriptionHandler(
        IServiceProvider serviceProvider,
        string handlerKey,
        IGlobalDomainEventTypeRegistry globalDomainEventTypeRegistry,
        IKeyedDomainEventTypeRegistry keyedDomainEventTypeRegistry,
        IEnumerable<IDomainEventBehavior> domainEventBehaviors,
        ILogger<ISubscriptionHandler> logger)
    {
        this._serviceProvider = serviceProvider;
        this._handlerKey = handlerKey;
        this._globalDomainEventTypeRegistry = globalDomainEventTypeRegistry;
        this._keyedDomainEventTypeRegistry = keyedDomainEventTypeRegistry;
        this._logger = logger;

        DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, IDomainEventBehavior behavior)
        {
            return (@event, cancellationToken) => behavior.HandleAsync(@event, next, cancellationToken);
        }

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

            if (this.IsDomainEventRegistrationMissing(domainEventWrapper!.DomainEventName))
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

    private bool IsDomainEventRegistrationMissing(string domainEventName)
    {
        return this._keyedDomainEventTypeRegistry.GetDomainEventType(domainEventName) == null
               && this._globalDomainEventTypeRegistry.GetDomainEventType(domainEventName) == null;
    }

    protected override Type? GetDomainEventType(string domainEventName)
    {
        return this._keyedDomainEventTypeRegistry.GetDomainEventType(domainEventName) ?? this._globalDomainEventTypeRegistry.GetDomainEventType(domainEventName)!;
    }

    protected override Type? GetDomainEventHandlerType(string domainEventName)
    {
        return this._keyedDomainEventTypeRegistry.GetDomainEventHandlerType(domainEventName) ?? this._globalDomainEventTypeRegistry.GetDomainEventHandlerType(domainEventName)!;
    }

    protected override object? ResolveDomainEventHandler(Type domainEventHandlerType)
    {
        // Microsoft DI does not offer an override for GetKeyedService (one) that supports receiving service type as a parameter
        var service = this._serviceProvider.GetKeyedServices(domainEventHandlerType, this._handlerKey).FirstOrDefault();
        return service ?? this._serviceProvider.GetService(domainEventHandlerType);
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

    private async Task<EventProcessingStatus> HandleDomainEventAsync(
        DomainEventWrapper domainEventWrapper,
        CancellationToken cancellationToken)
    {
        var handler = this.BuildDomainEventHandler(domainEventWrapper, cancellationToken);

        if (handler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(domainEventWrapper.DomainEventName);
            return EventProcessingStatus.Rejected;
        }

        await handler().ConfigureAwait(false);
        return EventProcessingStatus.Handled;
    }
}