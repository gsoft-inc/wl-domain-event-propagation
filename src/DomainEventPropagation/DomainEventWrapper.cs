using System.Text.Json;
using System.Text.Json.Nodes;

namespace Workleap.DomainEventPropagation;

internal class DomainEventWrapper
{
    private const string DomainEventTypeKey = "__type";
    private const string MetadataKey = "__metadata";

    private readonly JsonNode _domainEvent;

    public DomainEventWrapper(JsonNode domainEvent)
    {
        this._domainEvent = domainEvent;
    }

    public DomainEventWrapper(JsonNode domainEvent, string domainEventType)
    {
        this._domainEvent = domainEvent;
        this.DomainEventType = domainEventType;
    }

    public string DomainEventType
    {
        get => this._domainEvent[DomainEventTypeKey]?.ToString() ?? string.Empty;
        set => this._domainEvent[DomainEventTypeKey] = value;
    }

    public Dictionary<string, string> Metadata
    {
        get => (this._domainEvent[MetadataKey] ??= new JsonObject()).Deserialize<Dictionary<string, string>>()!;
        set => this._domainEvent[MetadataKey] = JsonSerializer.SerializeToNode(value);
    }
}