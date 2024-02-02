using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal static partial class LoggerExtensions
{
    [LoggerMessage(23, LogLevel.Warning, "Failed to pull CloudEvents from the Event Grid topic {topicName} on subscription {subscription} with reason: {reason}")]
    public static partial void CloudEventCannotBeReceived(this ILogger logger, string topicName, string subscription, string reason);

    [LoggerMessage(24, LogLevel.Warning, "Failed to handle event {EventId} {EventName} with reason {Reason}")]
    public static partial void EventHandlingFailed(this ILogger logger, string eventName, string eventId, string reason);

    [LoggerMessage(25, LogLevel.Error, "Received ill-formed CloudEvent with Id {EventId}")]
    public static partial void IllFormedCloudEvent(this ILogger logger, string eventId);
}