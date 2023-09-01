using System.Text.Json;
using System.Text.Json.Nodes;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventWrapper
{
    private const string NameKey = "__name";
    private const string MetadataKey = "__metadata";

    public DomainEventWrapper(JsonNode domainEventRawJson)
    {
        this.RawJson = domainEventRawJson;
    }

    public DomainEventWrapper(JsonNode domainEventRawJson, string domainEventName)
    {
        this.RawJson = domainEventRawJson;
        this.DomainEventName = domainEventName;
    }

    public JsonNode RawJson { get; }

    public string DomainEventName
    {
        get => this.RawJson[NameKey]?.ToString() ?? string.Empty;
        private set => this.RawJson[NameKey] = value;
    }

    public Dictionary<string, string> Metadata
    {
        get => (this.RawJson[MetadataKey] ??= new JsonObject()).Deserialize<Dictionary<string, string>>()!;
    }

    public object Unwrap(Type returnType)
    {
        return this.RawJson.Deserialize(returnType) ?? throw new ArgumentException("The event cannot be deserialized from JSON");
    }

    public static DomainEventWrapper Wrap<T>(T domainEvent)
        where T : IDomainEvent
    {
        var eventName = DomainEventNameCache.GetName<T>();
        var serializedEvent = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType())
            ?? throw new ArgumentException("The event cannot be serialized to JSON");

        return new DomainEventWrapper(serializedEvent, eventName);
    }
}