using Microsoft.Extensions.Logging;

namespace Workleap.DomainEventPropagation;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Failed to pull CloudEvents from the Event Grid topic {topicName} on subscription {subscription} with reason: {reason}")]
    public static partial void CloudEventCannotBeReceived(this ILogger logger, string topicName, string subscription, string reason);
}