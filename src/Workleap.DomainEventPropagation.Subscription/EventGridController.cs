using System.Diagnostics.CodeAnalysis;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// Entrypoint class that will handle EventGrid events (domain or subscription)
/// Will call the following classes
/// 1. IEventGridRequestHandler
/// 2.A ISubscriptionEventGridWebhookHandler (if subscription system event)
/// 2.B IDomainEventGridWebhookHandler (if domain event)
/// 3. ISubscriptionTopicValidator (in either case)
/// 4. ServiceProvider (use DI and reflection to resolve IDomainEventHandler)
/// </summary>
[ExcludeFromCodeCoverage]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class EventGridController : ControllerBase
{
    private readonly IEventGridRequestHandler _eventGridRequestHandler;

    public EventGridController(IEventGridRequestHandler eventGridRequestHandler)
    {
        this._eventGridRequestHandler = eventGridRequestHandler;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("eventgrid/domainevents")]
    public async Task<ActionResult> PostDomainEvent([FromBody] object requestContent, CancellationToken cancellationToken)
    {
        return await this.HandleRequestAsync(requestContent, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("eventgrid/systemevents")]
    public async Task<ActionResult> PostSystemEvent([FromBody] object requestContent, CancellationToken cancellationToken)
    {
        return await this.HandleRequestAsync(requestContent, cancellationToken);
    }

    private async Task<ActionResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken)
    {
        var requestTelemetry = HttpContext.Features.Get<RequestTelemetry>();

        var result = await this._eventGridRequestHandler.HandleRequestAsync(requestContent, cancellationToken, requestTelemetry: requestTelemetry);

        if (result.EventGridRequestType == EventGridRequestType.Subscription)
        {
            return this.Ok(result.Response);
        }

        return this.Ok();
    }
}