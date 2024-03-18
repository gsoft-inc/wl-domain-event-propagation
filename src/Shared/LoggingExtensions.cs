using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

// High-performance logging to prevent too many allocations
// https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "The cloud event with subject {Subject} could not be wrapped into Domain Event")]
    public static partial void CloudEventNotSerializedToWrapper(this ILogger logger, string subject);
    
    [LoggerMessage(2, LogLevel.Information, "The domain event type {DomainEventName} could not be found for event with subject {Subject}")]
    public static partial void EventDomainTypeNotRegistered(this ILogger logger, string domainEventName, string subject);

    [LoggerMessage(3, LogLevel.Information, "A handler for the domain event {DomainEventName} was not registered")]
    public static partial void EventDomainHandlerNotRegistered(this ILogger logger, string domainEventName);
    
    [LoggerMessage(4, LogLevel.Warning, "The event with {eventId} {EventName} will be rejected.")]
    public static partial void EventWillBeRejected(this ILogger logger, string eventId, string eventName, Exception ex);
    
    [LoggerMessage(5, LogLevel.Warning, "The event with {eventId} {EventName} will be released.")]
    public static partial void EventWillBeReleased(this ILogger logger, string eventId, string eventName, Exception ex);
    
    [LoggerMessage(23, LogLevel.Warning, "Failed to pull CloudEvents from the Event Grid topic {topicName} on subscription {subscription}")]
    public static partial void CloudEventCannotBeReceived(this ILogger logger, string topicName, string subscription, Exception ex);
    
    [LoggerMessage(24, LogLevel.Warning, "Failed to acknowledge CloudEvents from the Event Grid topic {topicName} on subscription {subscription}")]
    public static partial void CloudEventCannotBeAcknowledged(this ILogger logger, string topicName, string subscription, Exception ex);

    [LoggerMessage(25, LogLevel.Warning, "Failed to release CloudEvents from the Event Grid topic {topicName} on subscription {subscription}")]
    public static partial void CloudEventCannotBeReleased(this ILogger logger, string topicName, string subscription, Exception ex);
    
    [LoggerMessage(26, LogLevel.Warning, "Failed to reject CloudEvents from the Event Grid topic {topicName} on subscription {subscription}")]
    public static partial void CloudEventCannotBeRejected(this ILogger logger, string topicName, string subscription, Exception ex);
}