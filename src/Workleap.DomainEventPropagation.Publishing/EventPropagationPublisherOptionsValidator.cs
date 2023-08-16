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

        if (string.IsNullOrWhiteSpace(options.TopicName))
        {
            return ValidateOptionsResult.Fail("A topic name is required");
        }

        if (options.TopicEndpoint is null or { Length: 0 })
        {
            return ValidateOptionsResult.Fail("A topic endpoint is required");
        }

        try
        {
            if (new Uri(options.TopicEndpoint).IsAbsoluteUri == false)
            {  
                return ValidateOptionsResult.Fail("The topic endpoint must be an absolute URI");
            }
        }
        catch (UriFormatException)
        {
            return ValidateOptionsResult.Fail("The topic endpoint must be an absolute URI");
        }

        return ValidateOptionsResult.Success;
    }
}