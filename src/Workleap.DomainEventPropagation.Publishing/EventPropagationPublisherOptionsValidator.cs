using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublisherOptionsValidator : IValidateOptions<EventPropagationPublisherOptions>
{
    private readonly IEnumerable<ITopicProvider> _topicProviders;

    public EventPropagationPublisherOptionsValidator(IEnumerable<ITopicProvider> topicProvider)
    {
        this._topicProviders = topicProvider;
    }

    public ValidateOptionsResult Validate(string name, EventPropagationPublisherOptions options)
    {
        if (options.TokenCredential is null && string.IsNullOrWhiteSpace(options.TopicAccessKey))
        {
            return ValidateOptionsResult.Fail($"A token credential or an access key is required");
        }

        return this._topicProviders.Any(x => x.GetAllTopicsNames().Contains(options.TopicName)) ?
            ValidateOptionsResult.Success :
            ValidateOptionsResult.Fail($"Unrecognized topic: {options.TopicName}");
    }
}