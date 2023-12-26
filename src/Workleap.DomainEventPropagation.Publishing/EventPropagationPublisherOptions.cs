using Azure.Core;

namespace Workleap.DomainEventPropagation;

public enum TopicType
{
    Default = 0,
    Namespace = 1,
}

public sealed class EventPropagationPublisherOptions
{
    internal const string SectionName = "EventPropagation:Publisher";

    internal const string ClientName = "EventPropagationClient";

    public string TopicAccessKey { get; set; } = string.Empty;

    public TokenCredential? TokenCredential { get; set; }

    public string TopicEndpoint { get; set; } = string.Empty;

    public string? TopicName { get; set; } = string.Empty;

    public TopicType TopicType { get; set; } = TopicType.Default;
}