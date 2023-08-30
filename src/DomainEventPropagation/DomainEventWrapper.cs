using System.Text.Json;

namespace Workleap.DomainEventPropagation;

internal class DomainEventWrapper : IDomainEvent
{
    public string DomainEventType { get; set; } = string.Empty;

    public JsonElement DomainEventJson { get; set; }

    public Dictionary<string, string> ExtensionAttributes { get; set; } = new();
}