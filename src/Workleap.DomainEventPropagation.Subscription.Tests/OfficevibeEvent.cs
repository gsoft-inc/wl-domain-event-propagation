using Workleap.DomainEventPropagation;

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
    public Task HandleDomainEventAsync(OfficevibeEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}