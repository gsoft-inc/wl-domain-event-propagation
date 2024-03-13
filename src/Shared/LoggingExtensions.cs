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
    
    [LoggerMessage(4, LogLevel.Information, "The event with {eventId} will be rejected.")]
    public static partial void EventWillBeRejected(this ILogger logger, string eventId, Exception ex);
    
    [LoggerMessage(5, LogLevel.Information, "The event with {eventId} will be released.")]
    public static partial void EventWillBeReleased(this ILogger logger, string eventId, Exception ex);
}