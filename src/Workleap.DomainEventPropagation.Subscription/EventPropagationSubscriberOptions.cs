using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Workleap.EventPropagation.Subscription;

public sealed class EventPropagationSubscriberOptions
{
    [Required]
    public IEnumerable<string> SubscribedTopics { get; set; } = Enumerable.Empty<string>();

}