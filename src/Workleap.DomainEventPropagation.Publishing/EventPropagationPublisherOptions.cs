using System.ComponentModel.DataAnnotations;
using GSoft.ComponentModel.DataAnnotations;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublisherOptions
{
    internal const string SectionName = "EventPropagation";

    [Required]
    public string TopicName { get; set; }

    [Required]
    public string TopicAccessKey { get; set; }

    [Required]
    [UrlOfKind(UriKind.Absolute)]
    public string TopicEndpoint { get; set; }

}