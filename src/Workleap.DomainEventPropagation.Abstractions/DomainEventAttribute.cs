﻿namespace Workleap.DomainEventPropagation;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventAttribute : Attribute
{
    public DomainEventAttribute(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Domain event name cannot be empty", nameof(name));
        }

        this.Name = name;
    }

    public string Name { get; }
}