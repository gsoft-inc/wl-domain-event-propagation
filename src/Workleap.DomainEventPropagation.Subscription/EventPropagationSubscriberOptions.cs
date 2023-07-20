using System.ComponentModel.DataAnnotations;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationSubscriberOptions
{
    public const string SectionName = "EventPropagation:Subscriber";

    [Required]
    public IList<string> SubscribedTopics { get; set; } = new List<string>();
}