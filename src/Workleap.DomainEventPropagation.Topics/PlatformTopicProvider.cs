namespace Workleap.DomainEventPropagation;

public sealed class PlatformTopicProvider : ITopicProvider
{
    private static readonly Dictionary<string, string> Topics = new()
    {
        { "Organization", "-organization-egt-" },
        { "Integrations", "-integrations-egt-" },
        { "Signup", "-signup-egt-" },
    };

    public IEnumerable<string> GetAllTopicsNames()
    {
        return Topics.Keys;
    }

    public IEnumerable<string> GetAllTopicValidationPatterns()
    {
        return Topics.Values;
    }

    public string GetTopicValidationPattern(string topicName)
    {
        return Topics[topicName];
    }
}