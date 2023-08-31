using System.Text.Json;
using System.Text.Json.Nodes;

namespace Workleap.DomainEventPropagation;

internal class DomainEventWrapper
{
    private const string NameKey = "__name";
    private const string MetadataKey = "__metadata";

    public DomainEventWrapper(JsonNode rawJson)
    {
        this.RawJson = rawJson;
    }

    public DomainEventWrapper(JsonNode rawJson, string domainEventName)
    {
        this.RawJson = rawJson;
        this.DomainEventName = domainEventName;
    }

    public JsonNode RawJson { get; }

    public string DomainEventName
    {
        get => this.RawJson[NameKey]?.ToString() ?? string.Empty;
        set => this.RawJson[NameKey] = value;
    }

    public Dictionary<string, string> Metadata
    {
        get => (this.RawJson[MetadataKey] ??= new JsonObject()).Deserialize<Dictionary<string, string>>()!;
        set => this.RawJson[MetadataKey] = JsonSerializer.SerializeToNode(value);
    }

    public static DomainEventWrapper Wrap<T>(T domainEvent)
        where T : IDomainEvent
    {
        var eventName = DomainEventNameCache.GetName<T>();
        var serializedEvent = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType())
            ?? throw new ArgumentException("The event cannot be serialized to JSON");
        return new DomainEventWrapper(serializedEvent, eventName);
    }

    public static T Unwrap<T>(DomainEventWrapper domainEventWrapper)
        where T : IDomainEvent
    {
        return domainEventWrapper.RawJson.Deserialize<T>()
            ?? throw new ArgumentException("The event cannot be deserialized from JSON");
    }
}