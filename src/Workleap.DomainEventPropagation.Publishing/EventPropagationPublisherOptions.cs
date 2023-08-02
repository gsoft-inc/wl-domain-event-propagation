using System.ComponentModel.DataAnnotations;
using Azure.Core;
using GSoft.ComponentModel.DataAnnotations;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublisherOptions
{
    internal const string SectionName = "EventPropagation:Publisher";

    internal const string ClientName = "EventPropagationClient";

    [Required]
    public string TopicName { get; init; }

    public string TopicAccessKey { get; init; }

    public TokenCredential? TokenCredential { get; set; }

    [Required]
    [UrlOfKind(UriKind.Absolute)]
    public string TopicEndpoint { get; init; }
}