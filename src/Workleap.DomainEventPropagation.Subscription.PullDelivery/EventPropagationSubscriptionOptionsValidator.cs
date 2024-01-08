using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation;

public class EventPropagationSubscriptionOptionsValidator : IValidateOptions<EventPropagationSubscriptionOptions>
{
    public ValidateOptionsResult Validate(string name, EventPropagationSubscriptionOptions options)
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

        if (string.IsNullOrWhiteSpace(options.TopicName))
        {
            return ValidateOptionsResult.Fail("A topic endpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.SubscriptionName))
        {
            return ValidateOptionsResult.Fail("A topic endpoint is required");
        }

        return ValidateOptionsResult.Success;
    }
}