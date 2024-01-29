﻿namespace Workleap.DomainEventPropagation;

internal interface IDomainEventTypeRegistry
{
    Type? GetDomainEventType(string domainEventName);

    Type? GetDomainEventHandlerType(string domainEventName);

    void RegisterDomainEvent(Type domainEventType);
}

internal interface IKeyedDomainEventTypeRegistry : IDomainEventTypeRegistry
{
}

internal interface IGlobalDomainEventTypeRegistry : IDomainEventTypeRegistry
{
}