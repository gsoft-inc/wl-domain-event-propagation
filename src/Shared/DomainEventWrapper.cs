using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventWrapper
{
    private const string MetadataKey = "__metadata";

    public DomainEventWrapper(EventGridEvent eventGridEvent)
    {
        this.RawJson = eventGridEvent.Data.ToObjectFromJson<JsonObject>();
        this.DomainEventName = eventGridEvent.EventType;
    }

    private DomainEventWrapper(JsonNode rawJson, string domainEventName)
    {
        this.RawJson = rawJson;
        this.DomainEventName = domainEventName;
    }

    public JsonNode RawJson { get; }

    public string DomainEventName { get; }

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
        var domainEventName = DomainEventNameCache.GetName<T>();
        var serializedEvent = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType())
            ?? throw new ArgumentException("The event cannot be serialized to JSON");

        return new DomainEventWrapper(serializedEvent, domainEventName);
    }
}