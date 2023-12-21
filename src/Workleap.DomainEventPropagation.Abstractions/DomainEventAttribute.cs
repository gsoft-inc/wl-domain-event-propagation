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

        if (eventSchema == null)
        {
            throw new ArgumentNullException(nameof(eventSchema));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Domain event name cannot be empty", nameof(name));
        }

        this.Name = name;
        this.Schema = eventSchema;
    }

    public string Name { get; }

    public EventSchema Schema { get; }
}