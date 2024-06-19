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

    /// <summary>
    /// This is the name of the underlying named options for <see cref="Azure.Messaging.EventGrid.EventGridPublisherClientOptions"/> and <see cref="Azure.Messaging.EventGrid.Namespaces.EventGridClientOptions"/>.
    /// They are used when creating the corresponding <see cref="Azure.Messaging.EventGrid.EventGridPublisherClient"/> and <see cref="Azure.Messaging.EventGrid.Namespaces.EventGridClient"/>.
    /// </summary>
    /// <example>
    /// Use this option name to customize the Event Grid clients.
    /// <code>
    /// services.Configure&lt;EventGridPublisherClientOptions&gt;(EventPropagationPublisherOptions.EventGridClientName, options =>
    /// {
    ///     // ...
    /// }).
    /// </code>
    /// </example>
    public const string EventGridClientName = "DomainEventPropagation";

    public string TopicAccessKey { get; set; } = string.Empty;

    public TokenCredential? TokenCredential { get; set; }

    public string TopicEndpoint { get; set; } = string.Empty;

    public string? TopicName { get; set; } = string.Empty;

    public TopicType TopicType { get; set; } = TopicType.Custom;
}