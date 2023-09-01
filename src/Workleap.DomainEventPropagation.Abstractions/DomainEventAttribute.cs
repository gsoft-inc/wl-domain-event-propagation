namespace Workleap.DomainEventPropagation;

// TODO documentation
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventAttribute : Attribute
{
    // TODO documentation
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

    // TODO documentation
    public string Name { get; }
}