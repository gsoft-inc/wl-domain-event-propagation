namespace Workleap.DomainEventPropagation;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventAttribute : Attribute
{
    public DomainEventAttribute(string name, EventSchema eventSchema = EventSchema.EventGridEvent)
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
        this.EventSchema = eventSchema;
    }

    public string Name { get; }

    public EventSchema EventSchema { get; }
}