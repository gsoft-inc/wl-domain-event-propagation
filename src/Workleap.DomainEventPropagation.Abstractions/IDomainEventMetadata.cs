namespace Workleap.DomainEventPropagation;

public interface IDomainEventMetadata
{
    string Id { get; set; }

    string Source { get; set; }

    string Type { get; set; }

    DateTimeOffset? Time { get; set; }

    string? DataSchema { get; set; }

    string? Subject { get; set; }

    IDictionary<string, object> ExtensionAttributes { get; }
}