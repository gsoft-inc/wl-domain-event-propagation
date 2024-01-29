namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventTypeRegistry : IGlobalDomainEventTypeRegistry, IKeyedDomainEventTypeRegistry
{
    private readonly Dictionary<string, Type> _nameToDomainEventTypeMapping = new Dictionary<string, Type>(StringComparer.Ordinal);
    private readonly Dictionary<string, Type> _nameToDomainEventHandlerTypeMapping = new Dictionary<string, Type>(StringComparer.Ordinal);

    public Type? GetDomainEventType(string domainEventName)
    {
        return this._nameToDomainEventTypeMapping.TryGetValue(domainEventName, out var type) ? type : null;
    }

    public Type? GetDomainEventHandlerType(string domainEventName)
    {
        return this._nameToDomainEventHandlerTypeMapping.TryGetValue(domainEventName, out var type) ? type : null;
    }

    public void RegisterDomainEvent(Type domainEventType)
    {
        var domainEventName = DomainEventNameCache.GetName(domainEventType);

        if (!this._nameToDomainEventTypeMapping.TryGetValue(domainEventName, out var otherDomainEventType))
        {
            this._nameToDomainEventTypeMapping[domainEventName] = domainEventType;
            this._nameToDomainEventHandlerTypeMapping[domainEventName] = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);
        }
        else if (otherDomainEventType != domainEventType)
        {
            throw new ArgumentException($"Two domain event types cannot have the same name '{domainEventName}': '{domainEventType.FullName}' and '{otherDomainEventType.FullName}'");
        }
    }
}