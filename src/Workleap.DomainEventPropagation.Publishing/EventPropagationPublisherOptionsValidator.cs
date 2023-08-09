using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

public sealed class EventPropagationPublisherOptionsValidator : IValidateOptions<EventPropagationPublisherOptions>
{
    public ValidateOptionsResult Validate(string name, EventPropagationPublisherOptions options)
    {
        if (options.TokenCredential is null && string.IsNullOrWhiteSpace(options.TopicAccessKey))
        {
            return ValidateOptionsResult.Fail("A token credential or an access key is required");
        }

        return ValidateOptionsResult.Success;
    }
}