using System.ComponentModel.DataAnnotations;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationSubscriberOptions
{
    public const string SectionName = "EventPropagation:Subscriber";

    [Required]
    public IEnumerable<string> SubscribedTopics { get; set; } = Enumerable.Empty<string>();
}