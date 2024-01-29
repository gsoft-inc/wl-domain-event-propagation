using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : BaseEventHandler, IDomainEventGridWebhookHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;
    private readonly ILogger<DomainEventGridWebhookHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        ILogger<DomainEventGridWebhookHandler> logger,
        IEnumerable<ISubscriptionDomainEventBehavior> subscriptionDomainEventBehaviors)
    {
        this._serviceProvider = serviceProvider;
        this._domainEventTypeRegistry = domainEventTypeRegistry;
        this._logger = logger;
        this._pipeline = subscriptionDomainEventBehaviors.Reverse().Aggregate((DomainEventHandlerDelegate)this.HandleDomainEventAsync, BuildPipeline);
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        var domainEventWrapper = new DomainEventWrapper(eventGridEvent);

        if (this.IsDomainEventRegistrationMissing(domainEventWrapper.DomainEventName))
        {
            this._logger.EventDomainTypeNotRegistered(domainEventWrapper.DomainEventName, eventGridEvent.Subject);
            return;
        }

        await this._pipeline(domainEventWrapper, cancellationToken).ConfigureAwait(false);
    }

    protected override object? ResolveDomainEventHandler(Type domainEventHandlerType)
    {
        return this._serviceProvider.GetService(domainEventHandlerType);
    }

    protected override Type? GetDomainEventType(string domainEventName)
    {
        return this._domainEventTypeRegistry.GetDomainEventType(domainEventName);
    }

    protected override Type? GetDomainEventHandlerType(string domainEventName)
    {
        return this._domainEventTypeRegistry.GetDomainEventHandlerType(domainEventName);
    }

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, ISubscriptionDomainEventBehavior pipeline)
    {
        return (events, cancellationToken) => pipeline.HandleAsync(events, next, cancellationToken);
    }

    private bool IsDomainEventRegistrationMissing(string domainEventName)
    {
        return this._domainEventTypeRegistry.GetDomainEventType(domainEventName) == null;
    }

    private async Task HandleDomainEventAsync(DomainEventWrapper domainEventWrapper, CancellationToken cancellationToken)
    {
        var handler = this.BuildDomainEventHandler(domainEventWrapper, cancellationToken);
        if (handler == null)
        {
            this._logger.EventDomainHandlerNotRegistered(domainEventWrapper.DomainEventName);
            return;
        }

        await handler().ConfigureAwait(false);
    }
}