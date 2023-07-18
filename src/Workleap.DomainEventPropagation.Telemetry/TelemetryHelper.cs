namespace Workleap.DomainEventPropagation;

internal static class TelemetryHelper
{
    public static string GetDomainEventTypes(IEnumerable<IDomainEvent> domainEvents)
    {
        var distinctDomainEventTypes = domainEvents.Select(x => x.GetType().FullName).Distinct();

        return string.Join(",", distinctDomainEventTypes);
    }
}