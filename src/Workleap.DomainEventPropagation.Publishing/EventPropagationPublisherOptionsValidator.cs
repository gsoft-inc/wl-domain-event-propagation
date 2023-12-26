using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

internal sealed class EventPropagationPublisherOptionsValidator : IValidateOptions<EventPropagationPublisherOptions>
{
    public ValidateOptionsResult Validate(string name, EventPropagationPublisherOptions options)
    {
        if (options.TokenCredential is null && string.IsNullOrWhiteSpace(options.TopicAccessKey))
        {
            return ValidateOptionsResult.Fail("A token credential or an access key is required");
        }

        if (string.IsNullOrWhiteSpace(options.TopicEndpoint))
        {
            return ValidateOptionsResult.Fail("A topic endpoint is required");
        }

        if (!Uri.TryCreate(options.TopicEndpoint, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("The topic endpoint must be an absolute URI");
        }

        if (options.TopicType == TopicType.Namespace && string.IsNullOrWhiteSpace(options.TopicName))
        {
            return ValidateOptionsResult.Fail("A topic name is required when using a namespace topic type");
        }

        return ValidateOptionsResult.Success;
    }
}