using Azure.Core;

namespace Workleap.DomainEventPropagation;

public class EventPropagationSubscriptionOptions
{
    internal const string DefaultSectionName = "EventPropagation:Subscription";

    public string TopicAccessKey { get; set; } = string.Empty;

    public TokenCredential? TokenCredential { get; set; }

    public string TopicEndpoint { get; set; } = string.Empty;

    public string TopicName { get; set; } = string.Empty;

    public string SubscriptionName { get; set; } = string.Empty;

    public int MaxDegreeOfParallelism { get; set; } = 1;

    /// <summary>
    /// Client side maximum retry count before sending the message to the dead-letter queue.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    public IReadOnlyCollection<TimeSpan>? RetryDelays { get; set; }
}