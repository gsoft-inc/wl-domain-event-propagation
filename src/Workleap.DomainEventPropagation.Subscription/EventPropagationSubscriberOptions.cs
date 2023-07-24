using System.ComponentModel.DataAnnotations;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationSubscriberOptions
{
    [Required]
    public IEnumerable<string> SubscribedTopics { get; set; } = Enumerable.Empty<string>();

}