using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

namespace Workleap.DomainEventPropagation.AzureSystemEvents;

/// <summary>
/// If you want to handle an Azure system event, a mapping between the system event name and the corresponding event data class must be added here.
/// Event data classes are part of the Azure.Messaging.EventGrid library.
/// </summary>
public static class EventTypeMapping
{
    private static IDictionary<string, Type> Mapping => new Dictionary<string, Type>
    {
        { SystemEventNames.MediaJobFinished, typeof(MediaJobFinishedEventData) },
        { SystemEventNames.MediaJobCanceled, typeof(MediaJobCanceledEventData) },
        { SystemEventNames.MediaJobErrored, typeof(MediaJobErroredEventData) },
    };

    public static bool TryGetEventDataTypeForEventType(string eventType, out Type? eventDataType)
    {
        return Mapping.TryGetValue(eventType, out eventDataType);
    }
}