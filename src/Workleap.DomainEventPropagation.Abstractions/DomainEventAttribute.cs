namespace Workleap.DomainEventPropagation;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventAttribute : Attribute
{
    public DomainEventAttribute(string name)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}