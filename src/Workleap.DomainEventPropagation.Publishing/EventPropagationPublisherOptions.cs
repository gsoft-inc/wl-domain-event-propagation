using Azure.Core;

namespace Workleap.DomainEventPropagation;

public enum TopicType
{
    Custom = 0,
    Namespace = 1,
}

public sealed class EventPropagationPublisherOptions
{
    internal const string SectionName = "EventPropagation:Publisher";

    // EventPropagationClient was the legacy name when only one kind of topic was supported
    internal const string CustomTopicClientName = "EventPropagationClient";

    internal const string NamespaceTopicClientName = "NamespaceTopicClient";

    public string TopicAccessKey { get; set; } = string.Empty;

    public TokenCredential? TokenCredential { get; set; }

    public string TopicEndpoint { get; set; } = string.Empty;

    public string? TopicName { get; set; } = string.Empty;

    public TopicType TopicType { get; set; } = TopicType.Custom;
}