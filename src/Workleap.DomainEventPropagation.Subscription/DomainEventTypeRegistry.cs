namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventTypeRegistry : IDomainEventTypeRegistry
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

        var isOVEvent = !domainEventType.IsAbstract &&
                        domainEventType.GetInterfaces().Any(i => i.FullName == "Officevibe.DomainEvents.IDomainEvent");

        if (!this._nameToDomainEventTypeMapping.TryGetValue(domainEventName, out var otherDomainEventType))
        {
            this._nameToDomainEventTypeMapping[domainEventName] = domainEventType;

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);
            this._nameToDomainEventHandlerTypeMapping[domainEventName] = handlerType;

            if (isOVEvent)
            {
                this._nameToDomainEventTypeMapping[domainEventType.FullName!] = domainEventType;
                this._nameToDomainEventHandlerTypeMapping[domainEventType.FullName!] = handlerType;
            }
        }
        else if (otherDomainEventType != domainEventType)
        {
            throw new ArgumentException($"Two domain event types cannot have the same name '{domainEventName}': '{domainEventType.FullName}' and '{otherDomainEventType.FullName}'");
        }
    }
}