using System.Collections;
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
        this.Metadata = new MedatadaHolder(this.RawJson);
    }

    private DomainEventWrapper(JsonNode rawJson, string domainEventName)
    {
        this.RawJson = rawJson;
        this.DomainEventName = domainEventName;
        this.Metadata = new MedatadaHolder(this.RawJson);
    }

    public JsonNode RawJson { get; }

    public string DomainEventName { get; }

    public MedatadaHolder Metadata { get; }

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

    public sealed class MedatadaHolder : IDictionary<string, string>
    {
        private readonly JsonObject _medatada;

        public MedatadaHolder(JsonNode data)
        {
            if (data[MetadataKey] == null)
            {
                data[MetadataKey] = new JsonObject();
            }

            this._medatada = (JsonObject)data[MetadataKey]!;
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<string> Values => throw new NotImplementedException();

        public int Count => this._medatada.Count;

        public bool IsReadOnly => false;

        public string this[string propertyName]
        {
            get => this._medatada[propertyName]?.GetValue<string>() ?? string.Empty;
            set => this._medatada[propertyName] = value;
        }

        public void Add(string key, string value)
        {
            this._medatada.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return this._medatada.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return this._medatada.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            if (this._medatada.TryGetPropertyValue(key, out var jsonNode))
            {
                value = jsonNode!.GetValue<string>();
                return true;
            }

            value = string.Empty;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var item in this._medatada)
            {
                yield return new KeyValuePair<string, string>(item.Key, item.Value!.GetValue<string>());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this._medatada.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return this._medatada.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return this._medatada.Remove(item.Key);
        }
    }
}