using Azure.Core;

namespace Workleap.DomainEventPropagation;

public class EventPropagationSubscriptionOptions
{
    internal const string SectionName = "EventPropagation:Subscription";

    public string TopicAccessKey { get; set; } = string.Empty;

    public TokenCredential? TokenCredential { get; set; }

    public string TopicEndpoint { get; set; } = string.Empty;

    public string TopicName { get; set; } = string.Empty;

    public string SubscriptionName { get; set; } = string.Empty;
}