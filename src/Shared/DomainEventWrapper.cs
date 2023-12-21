using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventWrapper
{
    private static readonly JsonSerializerOptions DomainEventWrapperSerializerOptions = new(JsonSerializerDefaults.Web);

    public DomainEventWrapper(EventGridEvent eventGridEvent)
    {
        this.Data = eventGridEvent.Data.ToObjectFromJson<JsonObject>();
        this.DomainEventName = eventGridEvent.EventType;
        this.DomainEventSchema = EventSchema.EventGridEvent;
    }

    public DomainEventWrapper(CloudEvent cloudEvent)
    {
        this.Data = cloudEvent.Data!.ToObjectFromJson<JsonObject>();
        this.DomainEventName = cloudEvent.Type;
        this.DomainEventSchema = EventSchema.CloudEvent;
    }

    private DomainEventWrapper(JsonObject data, string domainEventName, EventSchema schema)
    {
        this.Data = data;
        this.DomainEventName = domainEventName;
        this.DomainEventSchema = schema;
    }

    public JsonObject Data { get; }

    public string DomainEventName { get; }

    public EventSchema DomainEventSchema { get; }

    public void SetMetadata(string key, string value)
    {
        if (this.DomainEventSchema == EventSchema.EventGridEvent)
        {
            this.Data[GetMetadataKey(key)] = value;
        }
    }

    public bool TryGetMetadata(string key, out string? value)
    {
        if (this.DomainEventSchema == EventSchema.EventGridEvent && this.Data.TryGetPropertyValue(GetMetadataKey(key), out var nodeValue) && nodeValue != null)
        {
            value = nodeValue.GetValue<string?>();
            return true;
        }

        value = null;
        return false;
    }

    private static string GetMetadataKey(string key) => "__" + key;

    public object Unwrap(Type returnType)
    {
        return this.Data.Deserialize(returnType, DomainEventWrapperSerializerOptions) ?? throw new ArgumentException("The event cannot be deserialized from JSON");
    }

    public static DomainEventWrapper Wrap<T>(T domainEvent)
        where T : IDomainEvent
    {
        var domainEventName = DomainEventNameCache.GetName<T>();
        var domainEventSchema = DomainEventSchemaCache.GetEventSchema<T>();
        var serializedEvent = (JsonObject?)JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType(), DomainEventWrapperSerializerOptions)
                              ?? throw new ArgumentException("The event cannot be serialized to JSON");

        return new DomainEventWrapper(serializedEvent, domainEventName, domainEventSchema);
    }
}