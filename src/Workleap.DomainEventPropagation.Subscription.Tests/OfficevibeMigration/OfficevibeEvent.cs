using Workleap.DomainEventPropagation;
using Workleap.DomainEventPropagation.Subscription.Tests;

// ReSharper disable CheckNamespace
namespace Officevibe.DomainEvents;

[DomainEvent("officevibe-event")]
public class OfficevibeEvent : IDomainEvent, Workleap.DomainEventPropagation.IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }

    public string DataVersion { get; } = "1.0";
}

public class OfficevibeDomainEventHandler : IDomainEventHandler<OfficevibeEvent>
{
    private readonly DomainEventGridWebhookHandlerIntegrationTests.DomainEventGridWebhookHandlerTestState _testState;

    public OfficevibeDomainEventHandler(DomainEventGridWebhookHandlerIntegrationTests.DomainEventGridWebhookHandlerTestState testState)
    {
        this._testState = testState;
    }

    public Task HandleDomainEventAsync(OfficevibeEvent domainEvent, CancellationToken cancellationToken)
    {
        this._testState.OfficevibeDomainEventHandlerCallCount++;
        return Task.CompletedTask;
    }
}