using Azure.Messaging;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// This class is a wrapper around <see cref="CloudEvent"/> to implement <see cref="IDomainEventMetadata"/>.
/// The goal is to expose the properties of a <see cref="CloudEvent"/> without adding Azure.Messaging as a dependency in the Abstractions project.
/// </summary>
internal sealed class DomainEventMetadataWrapper : IDomainEventMetadata
{
    private readonly CloudEvent _cloudEvent;

    public DomainEventMetadataWrapper(CloudEvent cloudEvent)
    {
        this._cloudEvent = cloudEvent;
    }

    public string Id
    {
        get => this._cloudEvent.Id;
        set => this._cloudEvent.Id = value;
    }

    public string Source
    {
        get => this._cloudEvent.Source;
        set => this._cloudEvent.Source = value;
    }

    public string Type
    {
        get => this._cloudEvent.Source;
        set => this._cloudEvent.Source = value;
    }

    public DateTimeOffset? Time
    {
        get => this._cloudEvent.Time;
        set => this._cloudEvent.Time = value;
    }

    public string? DataSchema
    {
        get => this._cloudEvent.DataSchema;
        set => this._cloudEvent.DataSchema = value;
    }

    public string? Subject
    {
        get => this._cloudEvent.Subject;
        set => this._cloudEvent.Subject = value;
    }

    public IDictionary<string, object> ExtensionAttributes => this._cloudEvent.ExtensionAttributes;
}