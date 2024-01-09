using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : BaseEventHandler, IDomainEventGridWebhookHandler
{
    private readonly ILogger<DomainEventGridWebhookHandler> _logger;
    private readonly DomainEventHandlerDelegate _pipeline;

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        ILogger<DomainEventGridWebhookHandler> logger,
        IEnumerable<ISubscriptionDomainEventBehavior> subscriptionDomainEventBehaviors)
        : base(serviceProvider, domainEventTypeRegistry)
    {
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

    private static DomainEventHandlerDelegate BuildPipeline(DomainEventHandlerDelegate next, ISubscriptionDomainEventBehavior pipeline)
    {
        return (events, cancellationToken) => pipeline.HandleAsync(events, next, cancellationToken);
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