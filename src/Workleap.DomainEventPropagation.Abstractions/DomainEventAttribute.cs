namespace Workleap.DomainEventPropagation;

// TODO documentation
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventAttribute : Attribute
{
    // TODO documentation
    public DomainEventAttribute(string name)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    // TODO documentation
    public string Name { get; }
}