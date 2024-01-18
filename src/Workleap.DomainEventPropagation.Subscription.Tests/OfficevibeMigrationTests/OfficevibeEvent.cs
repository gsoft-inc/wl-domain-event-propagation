using Workleap.DomainEventPropagation;
using Workleap.DomainEventPropagation.Subscription.Tests.OfficevibeMigrationTests;

// ReSharper disable CheckNamespace
namespace Officevibe.DomainEvents;

public interface IDomainEvent
{
    string DataVersion { get; }
}

[DomainEvent("test")]
public class OfficevibeEvent : IDomainEvent, Workleap.DomainEventPropagation.IDomainEvent
{
    public string Text { get; set; } = string.Empty;

    public int Number { get; set; }

    public string DataVersion { get; } = "1.0";
}

public class OfficevibeDomainEventHandler : IDomainEventHandler<OfficevibeEvent>
{
    private readonly SubscriberMigrationTestState _testState;

    public OfficevibeDomainEventHandler(SubscriberMigrationTestState testState)
    {
        this._testState = testState;
    }

    public Task HandleDomainEventAsync(OfficevibeEvent domainEvent, CancellationToken cancellationToken)
    {
        this._testState.OfficevibeDomainEventHandlerCallCount++;
        return Task.CompletedTask;
    }
}