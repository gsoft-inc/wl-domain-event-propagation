using System.Collections.Generic;

namespace Workleap.DomainEventPropagation
{
    public interface ITopicProvider
    {
        IEnumerable<string> GetAllTopicsNames();

        IEnumerable<string> GetAllTopicValidationPatterns();

        string GetTopicValidationPattern(string topicName);
    }
}