using System.Text.Json;

namespace Workleap.DomainEventPropagation;

public class DomainEventWrapper : IDomainEvent
{
    public string DomainEventType { get; set; }

    public JsonElement DomainEventJson { get; set; }

    public Dictionary<string, string> ExtensionAttributes { get; set; } = new();
}