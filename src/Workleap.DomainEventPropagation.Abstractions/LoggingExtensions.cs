using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

// High-performance logging to prevent too many allocations
// https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "The domain event type {DomainEventName} could not be found for event with subject {Subject}")]
    public static partial void EventDomainTypeNotRegistered(this ILogger logger, string domainEventName, string subject);

    [LoggerMessage(2, LogLevel.Information, "A handler for the domain event {DomainEventName} was not registered")]
    public static partial void EventDomainHandlerNotRegistered(this ILogger logger, string domainEventName);
}