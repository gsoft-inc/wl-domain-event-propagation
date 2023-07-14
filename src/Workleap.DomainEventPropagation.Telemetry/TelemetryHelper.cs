using Newtonsoft.Json;

namespace Workleap.DomainEventPropagation;

internal static class TelemetryHelper
{
    private const string TelemetryCorrelationIdPropertyName = "telemetryCorrelationId";
    private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };

    public static string GetDomainEventTypes(IEnumerable<IDomainEvent> domainEvents)
    {
        var distinctDomainEventTypes = domainEvents.Select(x => x.GetType().FullName).Distinct();

        return string.Join(",", distinctDomainEventTypes);
    }

    public static string AddOperationTelemetryCorrelationIdToSerializedObject(string serializedObject, string telemetryCorrelationId)
    {
        if (string.IsNullOrEmpty(serializedObject))
        {
            return serializedObject;
        }

        var endBrace = serializedObject.LastIndexOf("}");
        var result = serializedObject.Substring(0, endBrace) + $",\"{TelemetryCorrelationIdPropertyName}\": \"{telemetryCorrelationId}\"}}";

        return result;
    }

    public static string GetOperationCorrelationIdFromSerializedObject(string serializedObject)
    {
        if (string.IsNullOrEmpty(serializedObject))
        {
            return null;
        }

        try
        {
            var deserializedEvent = JsonConvert.DeserializeObject<TelemetryCorrelationPoco>(serializedObject, SerializerSettings);
            return deserializedEvent?.TelemetryCorrelationId;
        }
        catch (Exception)
        {
            // fallback on string manipulation if JsonConvert fails
            return GetCorrelationIdFromString(serializedObject);
        }
    }

    public static string GetCorrelationIdFromString(string serializedObject)
    {
        if (string.IsNullOrEmpty(serializedObject))
        {
            return null;
        }

        if (!serializedObject.Contains(TelemetryCorrelationIdPropertyName))
        {
            return null;
        }

        try
        {
            var indexOf = serializedObject.IndexOf(TelemetryCorrelationIdPropertyName);
            var propertySectionOfString = serializedObject.Substring(indexOf, serializedObject.Length - indexOf - 1);
            return propertySectionOfString.Replace("\"", "").Replace("{", "").Replace("}", "").Replace(":", "").Replace(TelemetryCorrelationIdPropertyName, "").Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class TelemetryCorrelationPoco
    {
        [JsonProperty("telemetryCorrelationId")]
        public string TelemetryCorrelationId { get; set; }
    }
}